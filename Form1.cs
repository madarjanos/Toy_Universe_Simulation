using System.Diagnostics;

namespace ToyUnivSimu
{
    public partial class Form1 : Form
    {
        // N-Body simulator
        NBodySimulation Nbody;
        const int NBODY_STEPS_BEFORE_RENDER = 100;

        // We will copy the NBody.X,Y,Z arrays (for drawing)
        double[] xcopy, ycopy, zcopy;

        // Bitmap for rendering
        Bitmap? renderBitmap = null;
        readonly object bitmapLock = new object();

        // 3D helper (projector)
        Projector3D projector3d;

        // zoom for drawing (user changes it)
        double zoomfactor = 0.8;

        // shifting of X,Y,Z coordinates before drawing (user changes it)
        double shiftX, shiftY, shiftZ = 0.0;

        // exit flag for backgroundWorker
        volatile bool workexit = false;

        // extra string to render onto the image (bitmap)
        string renderstr = "";

        // 3D / 2D toggle flag (for thread safety)
        volatile bool use3D = true;

        // Save PNG files flag (for thread safety)
        volatile bool isSavePNG;

        // Starting
        public Form1()
        {
            InitializeComponent();

            // Start NBody simu
            Nbody = new NBodySimulation();

            // Generate arrays for storing X,Y,Z for drawing
            int N = Nbody.X.Length;
            xcopy = new double[N];
            ycopy = new double[N];
            zcopy = new double[N];

            // Create 3D helper object (swetup eye position, orientation, etc.)
            //  Note that the setup is hard-coded into the Projector3D for simplicity
            //  (so we do not use the optional_setup argument)
            projector3d = new Projector3D();

            // Put current folder into textbox
            textBoxFolder.Text = System.Environment.CurrentDirectory;

            /* Background worker will be started by user button */
        }

        // ---------------------------------------------------------------
        // Main form closed (by user / system)
        // ---------------------------------------------------------------
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Signal shutdown to backgorund worker
            workexit = true;
        }

        // ---------------------------------------------------------------
        // Paint event (show the pre-rendered bitmap)
        // ---------------------------------------------------------------

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // Draw rendered bitmap onto picuterbox (without rescaling)
            lock (bitmapLock)
            {
                if (renderBitmap != null)
                    e.Graphics.DrawImageUnscaled(renderBitmap, 0, 0);
            }
        }

        // ---------------------------------------------------------------
        // Main drawing procedure
        //   1. copies + shifts particle data
        //   2. renders particles (and frame) into an off-screen Bitmap
        //   3. overlays texts onto that Bitmap
        //   4. swaps the new Bitmap in and triggers a Paint refresh
        // ---------------------------------------------------------------

        private int pngcount = 0; // PNG file counter
        private int renderstr_counter = 0; // how much frames until show renderstr_save
        private string renderstr_save = ""; // special text to show (user commands)
        void DrawProcedure(bool savePNG)
        {
            //Fixed 640x640 bitmap is generated always
            // (even if picturebox has different size)
            int w = 640;
            int h = 640;

            // 1. Copy particle positions
            Nbody.X.CopyTo(xcopy, 0);
            Nbody.Y.CopyTo(ycopy, 0);
            Nbody.Z.CopyTo(zcopy, 0);

            // 2. Apply user shift (pacman-wrapped if needed)
            if (Nbody.iswrapped)
            {
                for (int i = 0; i < xcopy.Length; i++)
                {
                    double Wrap(double x) => x - Math.Floor(x);
                    xcopy[i] = Wrap(xcopy[i] + shiftX);
                    ycopy[i] = Wrap(ycopy[i] + shiftY);
                    zcopy[i] = Wrap(zcopy[i] + shiftZ);
                }
            }
            else
            {
                for (int i = 0; i < xcopy.Length; i++)
                {
                    xcopy[i] += shiftX;
                    ycopy[i] += shiftY;
                    zcopy[i] += shiftZ;
                }
            }

            // 3. Render into a new Bitmap 
            Bitmap newBitmap = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(newBitmap))
            {
                // Graphical part
                g.Clear(Color.Black);
                if (use3D)
                    Draw3D(g, w, h);
                else
                    Draw2D(g, w, h);

                // 4. Put main information string onto the bitmap
                string mode = use3D ? "3D" : "2D (X-Y)";
                string text = $"Idõlépés: {Nbody.timestep} " +
                    $"Skálafaktor: {Nbody.scale:0.0} " +
                    $"Részecskék száma: {xcopy.Length} " +
                    $"[{mode}]";
                using Font font = new Font("Consolas", 10f);
                using SolidBrush textBrush = new SolidBrush(Color.White);
                var size = g.MeasureString(text, font);
                int centerx = (w - (int)size.Width) / 2;
                g.DrawString(text, font, textBrush, centerx, 4);

                // .. and some info about zooming / shift change, etc.
                if (renderstr != "")
                {
                    renderstr_save = renderstr;
                    renderstr = "";
                    renderstr_counter = 24;
                }
                if (renderstr_counter > 0)
                {
                    int c = (renderstr_counter * 255) / 24;
                    using SolidBrush textBrush2 = new SolidBrush(Color.FromArgb(c, c, c));
                    size = g.MeasureString(renderstr_save, font);
                    centerx = (w - (int)size.Width) / 2;
                    g.DrawString(renderstr_save, font, textBrush2, centerx, 20);
                    renderstr_counter--;
                    if (renderstr_counter == 0) renderstr_save = "";
                }
            }

            // 5. Save bitmap to PNG file (if asked)
            if (savePNG)
            {
                string msg;
                string fname = textBoxFolder.Text +
                    "frame" + pngcount.ToString("0000") + ".png";
                try
                {
                    newBitmap.Save(fname);
                    msg = "[OK] PNG saved " + fname;
                    pngcount++; //only increase if success
                }
                catch (Exception ex)
                {
                    msg = "[!] PNG save error " + fname + " - " + ex.Message;
                }
                backgroundWorker1.ReportProgress(0, msg);
            }

            // 6. Swap bitmaps and dispose the old one
            Bitmap? oldBitmap;
            lock (bitmapLock)
            {
                oldBitmap = renderBitmap;
                renderBitmap = newBitmap;
            }
            oldBitmap?.Dispose();
            //--- End of Drawing ---

            // Signal PictureBox to refresh (it will run in the form thread)
            pictureBox1.Invalidate();
        }

        // ---------------------------------------------------------------
        // 2D rendering
        // ---------------------------------------------------------------
        void Draw2D(Graphics g, int w, int h)
        {
            double scale = zoomfactor * Nbody.scale;
            double Rescale(double x) => ((x - 0.5) * scale) + 0.5;

            // Unit-square border for pacman universe
            if (Nbody.iswrapped)
            {
                using Pen pen = new Pen(Color.Red, 1);
                double[] xx = { 0.0, 1.0, 1.0, 0.0 };
                double[] yy = { 0.0, 0.0, 1.0, 1.0 };
                int[] picx = new int[4];
                int[] picy = new int[4];
                for (int i = 0; i < 4; i++)
                {
                    picx[i] = (int)Math.Round(Rescale(xx[i]) * w);
                    picy[i] = (int)Math.Round(Rescale(yy[i]) * h);
                }
                for (int i = 0; i < 4; i++)
                {
                    g.DrawLine(pen,
                        new Point(picx[i], picy[i]),
                        new Point(picx[(i + 1) % 4], picy[(i + 1) % 4]));
                }
            }

            // Particles (X-Y plane projection)
            using SolidBrush brush = new SolidBrush(Color.White);
            for (int i = 0; i < xcopy.Length; i++)
            {
                int px = (int)(Rescale(xcopy[i]) * w);
                int py = (int)(Rescale(ycopy[i]) * h);
                g.FillRectangle(brush, px, py, 2, 2);
            }
        }

        // ---------------------------------------------------------------
        // 3D perspective rendering
        // ---------------------------------------------------------------
        void Draw3D(Graphics g, int w, int h)
        {
            double scale = zoomfactor * Nbody.scale;
            double Rescale(double x) => ((x - 0.5) * scale) + 0.5;

            // Unit-cube border for pacman universe
            if (Nbody.iswrapped)
            {
                double[,] corners =
                {
                    {0,0,0}, {1,0,0}, {1,1,0}, {0,1,0},
                    {0,0,1}, {1,0,1}, {1,1,1}, {0,1,1}
                };
                int[,] edges =
                {
                    {0,1},{1,2},{2,3},{3,0}, //top
					{4,5},{5,6},{6,7},{7,4}, //bottom
					{0,4},{1,5},{2,6},{3,7}  //verticals
				};

                var proj = new (double sx, double sy)[8];
                for (int i = 0; i < 8; i++)
                    proj[i] = projector3d.Project3D(
                        Rescale(corners[i, 0]), Rescale(corners[i, 1]), Rescale(corners[i, 2]),
                        w, h);

                using Pen pen = new Pen(Color.Red, 1);
                for (int ei = 0; ei < 12; ei++)
                {
                    var (x1, y1) = proj[edges[ei, 0]];
                    var (x2, y2) = proj[edges[ei, 1]];
                    if (!double.IsNaN(x1) && !double.IsNaN(x2))
                        g.DrawLine(pen, (int)x1, (int)y1, (int)x2, (int)y2);
                }
            }

            // Draw particles
            using SolidBrush brush = new SolidBrush(Color.White);
            for (int i = 0; i < xcopy.Length; i++)
            {
                var (sx, sy) = projector3d.Project3D(
                    Rescale(xcopy[i]), Rescale(ycopy[i]), Rescale(zcopy[i]),
                    w, h);
                if (!double.IsNaN(sx))
                    g.FillRectangle(brush, (int)sx, (int)sy, 2, 2);
            }
        }

        // ---------------------------------------------------------------
        // Background worker - runs the simulation indefinitely
        // ---------------------------------------------------------------

        private bool workpause = false;
        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {

            while (workexit == false)
            {
                // If paused: Sleep and re-draw (e.g. if user changed the zoom)
                while (workpause && !workexit)
                {
                    // Small sleep (20 fps)
                    Thread.Sleep(50);
                    // Render image (but not save png)
                    DrawProcedure(false);
                }
                if (workexit) break;

                // Do simulation steps (e.g. 100 steps)
                var watch = new Stopwatch();
                watch.Start();
                {
                    Nbody.Run(NBODY_STEPS_BEFORE_RENDER);
                }
                watch.Stop();
                string s = watch.Elapsed.Milliseconds.ToString() + " msec";
                backgroundWorker1.ReportProgress(0, s);

                // Render image (and save PNG if checked)
                DrawProcedure(isSavePNG);

            }
            // Finish simulation nicely
            Nbody.Dispose();
        }

        // Show simulation report string (info) in thread safe way
        private void backgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            label1.Text = e.UserState as string;
        }

        // ---------------------------------------------------------------
        // User buttons - run control, zoom and shift
        // ---------------------------------------------------------------

        private void checkBoxSavePNG_CheckedChanged(object sender, EventArgs e)
        {
            isSavePNG = checkBoxSavePNG.Checked;
        }

        private void buttonRuncontrol_Click(object sender, EventArgs e)
        {
            // If first use (start):
            if (buttonRuncontrol.Text == "Start")
            {
                // Start Background worker which run the simulation
                backgroundWorker1.RunWorkerAsync(null);
                // new function of button: pause/resume
                buttonRuncontrol.Text = "Pause";
                return;
            }
            // Pause / Resume
            if (buttonRuncontrol.Text == "Pause")
            {
                workpause = true;
                buttonRuncontrol.Text = "Resume";
            }
            else
            {
                workpause = false;
                buttonRuncontrol.Text = "Pause";
            }
        }

        private void buttonZoomIn_Click(object sender, EventArgs e)
        {
            zoomfactor *= 1.5;
            renderstr = "(Nagyít)";
        }

        private void buttonZoomOut_Click(object sender, EventArgs e)
        {
            zoomfactor /= 1.5;
            renderstr = "(Kicsinyít)";
        }

        private void buttonShiftYPlus_Click(object sender, EventArgs e)
        {
            shiftY += 0.05;
            if (Nbody.iswrapped && shiftY > 1.0) shiftY = 0.05;
            renderstr = "(Eltol Y+)";
        }

        private void buttonShitfYMinus_Click(object sender, EventArgs e)
        {
            shiftY -= 0.05;
            if (Nbody.iswrapped && shiftY < 0.0) shiftY = 0.95;
            renderstr = "(Eltol Y-)";
        }

        private void buttonShiftXPlus_Click(object sender, EventArgs e)
        {
            shiftX += 0.05;
            if (Nbody.iswrapped && shiftX > 1.0) shiftX = 0.05;
            renderstr = "(Eltol X+)";
        }

        private void buttonShiftXMinus_Click(object sender, EventArgs e)
        {
            shiftX -= 0.05;
            if (Nbody.iswrapped && shiftX < 0.0) shiftX = 0.95;
            renderstr = "(Eltol X-)";
        }

        private void buttonShiftZPlus_Click(object sender, EventArgs e)
        {
            shiftZ += 0.05;
            if (Nbody.iswrapped && shiftZ > 1.0) shiftZ = 0.05;
            renderstr = "(Eltol Z+)";
        }

        private void buttonShiftZMinus_Click(object sender, EventArgs e)
        {
            shiftZ -= 0.05;
            if (Nbody.iswrapped && shiftZ < 0.0) shiftZ = 0.95;
            renderstr = "(Eltol Z-)";
        }

        private void button2D3D_Click(object sender, EventArgs e)
        {
            use3D = !use3D;
            renderstr = "(Vált 2D/3D)";
        }
    }
}
