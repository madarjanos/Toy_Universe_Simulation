namespace ToyUnivSimu
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            pictureBox1 = new PictureBox();
            backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            label1 = new Label();
            buttonRuncontrol = new Button();
            checkBoxSavePNG = new CheckBox();
            buttonZoomIn = new Button();
            buttonZoomOut = new Button();
            buttonShiftXPlus = new Button();
            buttonShiftXMinus = new Button();
            buttonShitfYPlus = new Button();
            buttonShitfYMinus = new Button();
            buttonShiftZPlus = new Button();
            buttonShiftZMinus = new Button();
            textBoxFolder = new TextBox();
            button2D3D = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = Color.Black;
            pictureBox1.Location = new Point(12, 69);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(640, 640);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            pictureBox1.Paint += pictureBox1_Paint;
            // 
            // backgroundWorker1
            // 
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.DoWork += backgroundWorker1_DoWork;
            backgroundWorker1.ProgressChanged += backgroundWorker1_ProgressChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 46);
            label1.Name = "label1";
            label1.Size = new Size(54, 20);
            label1.TabIndex = 13;
            label1.Text = "(Info...)";
            // 
            // buttonRuncontrol
            // 
            buttonRuncontrol.Location = new Point(679, 70);
            buttonRuncontrol.Name = "buttonRuncontrol";
            buttonRuncontrol.Size = new Size(104, 29);
            buttonRuncontrol.TabIndex = 1;
            buttonRuncontrol.Text = "Start";
            buttonRuncontrol.UseVisualStyleBackColor = true;
            buttonRuncontrol.Click += buttonRuncontrol_Click;
            // 
            // checkBoxSavePNG
            // 
            checkBoxSavePNG.AutoSize = true;
            checkBoxSavePNG.Location = new Point(31, 12);
            checkBoxSavePNG.Name = "checkBoxSavePNG";
            checkBoxSavePNG.Size = new Size(122, 24);
            checkBoxSavePNG.TabIndex = 11;
            checkBoxSavePNG.Text = "Save PNGs to:";
            checkBoxSavePNG.TextAlign = ContentAlignment.MiddleRight;
            checkBoxSavePNG.UseVisualStyleBackColor = true;
            checkBoxSavePNG.CheckedChanged += checkBoxSavePNG_CheckedChanged;
            // 
            // buttonZoomIn
            // 
            buttonZoomIn.Location = new Point(679, 113);
            buttonZoomIn.Name = "buttonZoomIn";
            buttonZoomIn.Size = new Size(104, 29);
            buttonZoomIn.TabIndex = 2;
            buttonZoomIn.Text = "Zoom in";
            buttonZoomIn.UseVisualStyleBackColor = true;
            buttonZoomIn.Click += buttonZoomIn_Click;
            // 
            // buttonZoomOut
            // 
            buttonZoomOut.Location = new Point(679, 148);
            buttonZoomOut.Name = "buttonZoomOut";
            buttonZoomOut.Size = new Size(104, 29);
            buttonZoomOut.TabIndex = 3;
            buttonZoomOut.Text = "Zoom out";
            buttonZoomOut.UseVisualStyleBackColor = true;
            buttonZoomOut.Click += buttonZoomOut_Click;
            // 
            // buttonShiftXPlus
            // 
            buttonShiftXPlus.Location = new Point(679, 195);
            buttonShiftXPlus.Name = "buttonShiftXPlus";
            buttonShiftXPlus.Size = new Size(104, 29);
            buttonShiftXPlus.TabIndex = 4;
            buttonShiftXPlus.Text = "Shit X+";
            buttonShiftXPlus.UseVisualStyleBackColor = true;
            buttonShiftXPlus.Click += buttonShiftXPlus_Click;
            // 
            // buttonShiftXMinus
            // 
            buttonShiftXMinus.Location = new Point(679, 230);
            buttonShiftXMinus.Name = "buttonShiftXMinus";
            buttonShiftXMinus.Size = new Size(104, 29);
            buttonShiftXMinus.TabIndex = 5;
            buttonShiftXMinus.Text = "Shit X-";
            buttonShiftXMinus.UseVisualStyleBackColor = true;
            buttonShiftXMinus.Click += buttonShiftXMinus_Click;
            // 
            // buttonShitfYPlus
            // 
            buttonShitfYPlus.Location = new Point(679, 265);
            buttonShitfYPlus.Name = "buttonShitfYPlus";
            buttonShitfYPlus.Size = new Size(104, 29);
            buttonShitfYPlus.TabIndex = 6;
            buttonShitfYPlus.Text = "Shit Y+";
            buttonShitfYPlus.UseVisualStyleBackColor = true;
            buttonShitfYPlus.Click += buttonShiftYPlus_Click;
            // 
            // buttonShitfYMinus
            // 
            buttonShitfYMinus.Location = new Point(679, 300);
            buttonShitfYMinus.Name = "buttonShitfYMinus";
            buttonShitfYMinus.Size = new Size(104, 29);
            buttonShitfYMinus.TabIndex = 7;
            buttonShitfYMinus.Text = "Shit Y-";
            buttonShitfYMinus.UseVisualStyleBackColor = true;
            buttonShitfYMinus.Click += buttonShitfYMinus_Click;
            // 
            // buttonShiftZPlus
            // 
            buttonShiftZPlus.Location = new Point(679, 335);
            buttonShiftZPlus.Name = "buttonShiftZPlus";
            buttonShiftZPlus.Size = new Size(104, 29);
            buttonShiftZPlus.TabIndex = 8;
            buttonShiftZPlus.Text = "Shit Z+";
            buttonShiftZPlus.UseVisualStyleBackColor = true;
            buttonShiftZPlus.Click += buttonShiftZPlus_Click;
            // 
            // buttonShiftZMinus
            // 
            buttonShiftZMinus.Location = new Point(679, 370);
            buttonShiftZMinus.Name = "buttonShiftZMinus";
            buttonShiftZMinus.Size = new Size(104, 29);
            buttonShiftZMinus.TabIndex = 9;
            buttonShiftZMinus.Text = "Shit Z-";
            buttonShiftZMinus.UseVisualStyleBackColor = true;
            buttonShiftZMinus.Click += buttonShiftZMinus_Click;
            // 
            // textBoxFolder
            // 
            textBoxFolder.Location = new Point(159, 10);
            textBoxFolder.Name = "textBoxFolder";
            textBoxFolder.Size = new Size(472, 27);
            textBoxFolder.TabIndex = 12;
            // 
            // button2D3D
            // 
            button2D3D.Location = new Point(679, 425);
            button2D3D.Name = "button2D3D";
            button2D3D.Size = new Size(104, 29);
            button2D3D.TabIndex = 10;
            button2D3D.Text = "2D/3D";
            button2D3D.UseVisualStyleBackColor = true;
            button2D3D.Click += button2D3D_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(795, 721);
            Controls.Add(button2D3D);
            Controls.Add(textBoxFolder);
            Controls.Add(buttonRuncontrol);
            Controls.Add(checkBoxSavePNG);
            Controls.Add(buttonShiftZMinus);
            Controls.Add(buttonShiftZPlus);
            Controls.Add(buttonShiftXMinus);
            Controls.Add(buttonShiftXPlus);
            Controls.Add(buttonShitfYMinus);
            Controls.Add(buttonShitfYPlus);
            Controls.Add(buttonZoomOut);
            Controls.Add(buttonZoomIn);
            Controls.Add(label1);
            Controls.Add(pictureBox1);
            Name = "Form1";
            Text = "Toy Universe simulation";
            FormClosed += Form1_FormClosed;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private Label label1;
        private Button buttonRuncontrol;
        private CheckBox checkBoxSavePNG;
        private Button buttonZoomIn;
        private Button buttonZoomOut;
        private Button buttonShiftXPlus;
        private Button buttonShiftXMinus;
        private Button buttonShitfYPlus;
        private Button buttonShitfYMinus;
        private Button buttonShiftZPlus;
        private Button buttonShiftZMinus;
        private TextBox textBoxFolder;
        private Button button2D3D;
    }
}
