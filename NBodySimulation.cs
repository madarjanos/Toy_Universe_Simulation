// *** IMPORTANT ***
// Define "_WRAPPED" if the toy universe is wrapped (Pacman style)
#define _WRAPPED 

using System;
using System.Threading;

class NBodySimulation : IDisposable
{
	// Simulation parameters
	const int RANDOM_SEED = 301;
    const int N = 2000;
    const double MASS_MIN = 0.1;
    const double MASS_MAX = 1.0;
	const double G = 1.0;
	const double DT = 2e-6;
	const double SOFTENING = 1e-3;
	const double EXPANSION_FACTOR = 100; // per time unit
#if _WRAPPED
    public readonly bool iswrapped = true;
#else
	public const bool iswrapped = false;
#endif

    //CPU dependent paramter (set to number of "performance" cores)
    const int NUM_THREADS = 4;

    // Expansion and time
    public double time = 0.0;
	public int timestep = 0;
	public double scale = 1.0;
	public double scale_prev = 1.0;

	// Particle state
	public double[] X, Y, Z, VelX, VelY, VelZ, Mass;
	public double[] Ax, Ay, Az;
	
	// Threads
	Thread[] threads;

	// Per-thread private (accelration) accumulator buffers
	double[][] TBufX, TBufY, TBufZ;

	// Work distribution
	int[] KickStart, KickEnd;       // equal slices for kick / drift / reduction
	int[] ForceStart, ForceEnd;     // work-balanced slices for force calculation

	// Synchronisation objects
	ManualResetEventSlim[] StartStep;
	CountdownEvent StepDone;
	Barrier BarrierAfterKickDrift;
	Barrier BarrierAfterForce;
	Barrier BarrierAfterReduction;
	
	// Flag for shutdown of Threads (only used in Dispose)
	volatile bool Shutdown = false;

	// --------------------------------------------------------------------
	// Initialization
	// --------------------------------------------------------------------
	public NBodySimulation()
	{
		var rng = new Random(RANDOM_SEED);

		// Particle arrays 
		X = new double[N]; Y = new double[N]; Z = new double[N];
		VelX = new double[N]; VelY = new double[N]; VelZ = new double[N];
		Mass = new double[N];
		Ax = new double[N]; Ay = new double[N]; Az = new double[N];

		// Random initial states
		for (int i = 0; i < N; i++)
		{
			double r;
			do
			{
				// Coordinates MUST be between 0-1 (for wrapped "pacman" unviverse)
				X[i] = rng.NextDouble();
				Y[i] = rng.NextDouble();
				Z[i] = rng.NextDouble();
				if (iswrapped) break;
				// if not wrapped than it is better if inside 0.5 radius sphere
				r = (X[i] - 0.5) * (X[i] - 0.5) + (Y[i] - 0.5) * (Y[i] - 0.5) +
					(Z[i] - 0.5) * (Z[i] - 0.5);
			} while (r > 0.5 * 0.5);
			// initial speed is zero
			VelX[i] = VelY[i] = VelZ[i] = 0.0;
			// inital masses
			Mass[i] = MASS_MIN + rng.NextDouble() * (MASS_MAX - MASS_MIN);
		}

		// Per-thread accumulator buffers
		TBufX = new double[NUM_THREADS][];
		TBufY = new double[NUM_THREADS][];
		TBufZ = new double[NUM_THREADS][];
		for (int t = 0; t < NUM_THREADS; t++)
		{
			TBufX[t] = new double[N];
			TBufY[t] = new double[N];
			TBufZ[t] = new double[N];
		}

		// Equal-particle slices for kick / drift / reduction
		KickStart = new int[NUM_THREADS];
		KickEnd = new int[NUM_THREADS];
		for (int t = 0; t < NUM_THREADS; t++)
		{
			KickStart[t] = t * N / NUM_THREADS;
			KickEnd[t] = (t + 1) * N / NUM_THREADS;
		}

		// Work-balanced row slices for force calculation
		// Pairs in rows [0, i):  P(i) = i*(2N - i - 1) / 2
		// Invert to find cut points so each thread gets equal pair count
		ForceStart = new int[NUM_THREADS];
		ForceEnd = new int[NUM_THREADS];
		double totalPairs = (double)N * (N - 1) / 2.0;
		double halfN = (2.0 * N - 1.0) / 2.0;
		int[] cuts = new int[NUM_THREADS + 1];
		cuts[0] = 0; cuts[NUM_THREADS] = N;
		for (int t = 1; t < NUM_THREADS; t++)
		{
			double k = t * totalPairs / NUM_THREADS;
			int row = (int)Math.Round(halfN - Math.Sqrt(halfN * halfN - 2.0 * k));
			cuts[t] = Math.Clamp(row, cuts[t - 1], N);
		}
		for (int t = 0; t < NUM_THREADS; t++)
		{
			ForceStart[t] = cuts[t];
			ForceEnd[t] = cuts[t + 1];
		}

		// Synchronisation objects
		StartStep = new ManualResetEventSlim[NUM_THREADS];
		for (int t = 0; t < NUM_THREADS; t++)
			StartStep[t] = new ManualResetEventSlim(false);

		StepDone = new CountdownEvent(NUM_THREADS);

		BarrierAfterKickDrift = new Barrier(NUM_THREADS);
		BarrierAfterForce = new Barrier(NUM_THREADS);
		BarrierAfterReduction = new Barrier(NUM_THREADS);

		// Start persistent worker threads
		threads = new Thread[NUM_THREADS];
 		for (int t = 0; t < NUM_THREADS; t++)
		{
			threads[t] = new Thread(WorkerThread)
			{
				IsBackground = true,
				Name = $"NBody-Worker-{t}"
			};
			threads[t].Start(t);
		}
	}

	// --------------------------------------------------------------------
	// Worker step functions
	// --------------------------------------------------------------------

	// Step 1: half-kick velocities + drift positions
	void DoHalfKickAndDrift(int t)
	{
		double half_dt = 0.5 * DT;
		int start = KickStart[t], end = KickEnd[t];
		for (int i = start; i < end; i++)
		{
			VelX[i] += half_dt * Ax[i];
			VelY[i] += half_dt * Ay[i];
			VelZ[i] += half_dt * Az[i];
#if _WRAPPED
			//"Pacman" wrapped coordinates:
			double Wrap(double x) => x - Math.Floor(x);
			X[i] = Wrap(X[i] + DT * VelX[i]);
			Y[i] = Wrap(Y[i] + DT * VelY[i]);
			Z[i] = Wrap(Z[i] + DT * VelZ[i]);
#else
			X[i] = X[i] + DT * VelX[i];
			Y[i] = Y[i] + DT * VelY[i];
			Z[i] = Z[i] + DT * VelZ[i];
#endif
		}

	}

    // Step 1B: After Step1 is the best point applying the expansion
    void DoExpansion(int t)
	{
        //Expansion decrease the velocities two times!
        // 1: Due to scale factoring (we do not change the coordinates directly)
        // 2: Physical peculiary velocity decrease due to space expansion (real physical effect)
        double scalechange_rec_sqr = (scale_prev / scale);
		scalechange_rec_sqr = scalechange_rec_sqr * scalechange_rec_sqr;

        int startix = KickStart[t], endix = KickEnd[t];
        for (int i = startix; i < endix; i++)
        {
            VelX[i] *= scalechange_rec_sqr;
            VelY[i] *= scalechange_rec_sqr;
            VelZ[i] *= scalechange_rec_sqr;
        }
    }

	// Step 2: force calculation into private accumulator buffers.
	// Thread t owns rows [ForceStart[t], ForceEnd[t]); full upper triangle per row.
	// Both +f (on i) and -f (on j) go into TBuf[t] only — no sharing, no races.
	void DoForceCalculation(int t)
	{
		// Calculated accelration collector arrays
		double[] lx = TBufX[t], ly = TBufY[t], lz = TBufZ[t];
		Array.Clear(lx, 0, N);
		Array.Clear(ly, 0, N);
		Array.Clear(lz, 0, N);

		// Universe expansion scaling
		//  We do not directly change the coordinates with expansion
		//  but rather scale the accelrations and velocities.
		//  It is faster and simpler
		double eps2_scaled = SOFTENING * SOFTENING / (scale * scale);
		double G_scaled = G / (scale * scale);

		// Maximum distance for gravitaion is calculated
		//   It is neccessary for wrapped "pacman" universe for isotropy
		//   For not wrapped universe we just define a large number
		double grav_radius;
		if (iswrapped)
			grav_radius = 0.5;
		else
			grav_radius = 20.0;

		// Main force-accelration calculation loop:
		int start_ix = ForceStart[t], end_ix = ForceEnd[t];
		for (int i = start_ix; i < end_ix; i++)
		{
			double xi = X[i], yi = Y[i], zi = Z[i];
			double mi = Mass[i];

			for (int j = i + 1; j < N; j++)
			{
#if _WRAPPED
				//"Pacman" wrapped coordinate differnces:
				double WrapDiff(double dx) => dx - Math.Floor(dx + 0.5);
				double dx = WrapDiff(X[j] - xi);
				double dy = WrapDiff(Y[j] - yi);
				double dz = WrapDiff(Z[j] - zi);
#else
				double dx = X[j] - xi;
				double dy = Y[j] - yi;
				double dz = Z[j] - zi;
#endif
				double dist2 = dx * dx + dy * dy + dz * dz + eps2_scaled;

				if (dist2 > grav_radius) continue;

				double invDist3 = 1.0 / (dist2 * Math.Sqrt(dist2));

				double fi = G_scaled * Mass[j] * invDist3;
				double fj = G_scaled * mi * invDist3;

				lx[i] += fi * dx; ly[i] += fi * dy; lz[i] += fi * dz;
				lx[j] -= fj * dx; ly[j] -= fj * dy; lz[j] -= fj * dz;
			}
		}
	}

	// Step 3: sum per-thread accumulator buffers into global Ax/Ay/Az.
	// Each thread sums its own particle slice across all NUM_THREADS buffers.
	void DoReduction(int t)
	{
		int start_ix = KickStart[t], end_ix = KickEnd[t];
		for (int i = start_ix; i < end_ix; i++)
		{
			double sumX = 0, sumY = 0, sumZ = 0;
			for (int k = 0; k < NUM_THREADS; k++)
			{
				sumX += TBufX[k][i];
				sumY += TBufY[k][i];
				sumZ += TBufZ[k][i];
			}
			Ax[i] = sumX;
			Ay[i] = sumY;
			Az[i] = sumZ;
		}
	}

	// Step 4: second half-kick velocities
	void DoSecondHalfKick(int t)
	{
		double half_dt = 0.5 * DT;
		int start_ix = KickStart[t], end_ix = KickEnd[t];
		for (int i = start_ix; i < end_ix; i++)
		{
			VelX[i] += half_dt * Ax[i];
			VelY[i] += half_dt * Ay[i];
			VelZ[i] += half_dt * Az[i];
		}
	}

	// --------------------------------------------------------------------
	// Worker thread(s) main loop
	// --------------------------------------------------------------------
	void WorkerThread(object? state)
	{
		if (state == null) { return; }
		int t = (int)state;

		while (true)
		{
			// Before Step 1: wait for Run() to signal start of next step
			StartStep[t].Wait();
			StartStep[t].Reset();

			if (Shutdown) return;

			// Step 1: half-kick + drift + scale expansion effect
			DoHalfKickAndDrift(t);
			DoExpansion(t);
			BarrierAfterKickDrift.SignalAndWait();

			// Step 2: force calculation
			DoForceCalculation(t);
			BarrierAfterForce.SignalAndWait();

			// Step 3: reduction
			DoReduction(t);
			BarrierAfterReduction.SignalAndWait();

			// Step 4: second half-kick
			DoSecondHalfKick(t);

			// Signal to Run() that this thread finished the step
			StepDone.Signal();
		}
	}

	// --------------------------------------------------------------------
	// Run - Execute <steps> number of simulation step
	// --------------------------------------------------------------------
	public void Run(int steps)
	{
		// Main simulation loop
		for (int step = 0; step < steps; step++)
		{
			//Signal all working threads to start
			StepDone.Reset(NUM_THREADS);
			for (int t = 0; t < NUM_THREADS; t++)
				StartStep[t].Set();

			//Wait until all working threads finished the step
			StepDone.Wait();

			//Time step (global)
			time += DT;
			timestep++;

			//Expansion scale factor step (global)
			scale_prev = scale;
			scale = scale + DT * EXPANSION_FACTOR;
		}
	}

	// --------------------------------------------------------------------
	// When finish: Nice shutdown of threads
	// --------------------------------------------------------------------

	private bool disposed = false;
	public void Dispose()
	{
		if (disposed) return;
		disposed = true;
		//Stop worker threads cleanly
		Shutdown = true;
		StepDone.Reset(NUM_THREADS);
		for (int t = 0; t < NUM_THREADS; t++)
			StartStep[t].Set();
		for (int t = 0; t < NUM_THREADS; t++)
			threads[t].Join();
	}

}
