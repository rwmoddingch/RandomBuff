namespace MultiLayerHelper
{
    partial class MainForm
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
            components = new System.ComponentModel.Container();
            toolTip1 = new ToolTip(components);
            button1 = new Button();
            listView1 = new ListView();
            button2 = new Button();
            textBox1 = new TextBox();
            label1 = new Label();
            SuspendLayout();
            // 
            // button1
            // 
            button1.AllowDrop = true;
            button1.Location = new Point(38, 41);
            button1.Name = "button1";
            button1.Size = new Size(155, 138);
            button1.TabIndex = 1;
            button1.Text = "Choose Image\r\n\r\n(or Drag)";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            button1.DragDrop += button1_DragDrop;
            button1.DragEnter += button1_DragEnter;
            // 
            // listView1
            // 
            listView1.Location = new Point(241, 41);
            listView1.Name = "listView1";
            listView1.Size = new Size(470, 283);
            listView1.TabIndex = 2;
            listView1.UseCompatibleStateImageBehavior = false;
            // 
            // button2
            // 
            button2.Location = new Point(38, 279);
            button2.Name = "button2";
            button2.Size = new Size(155, 45);
            button2.TabIndex = 3;
            button2.Text = "Merge";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(119, 219);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(74, 27);
            textBox1.TabIndex = 4;
            textBox1.Text = "300";
            textBox1.TextAlign = HorizontalAlignment.Right;
            textBox1.Validated += textBox1_Validated;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(38, 222);
            label1.Name = "label1";
            label1.Size = new Size(61, 20);
            label1.TabIndex = 5;
            label1.Text = "Width :";
            // 
            // MainForm
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(9F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(748, 362);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Controls.Add(button2);
            Controls.Add(listView1);
            Controls.Add(button1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "MainForm";
            Text = "MultiLayerHelper";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ToolTip toolTip1;
        private Button button1;
        private ListView listView1;
        private Button button2;
        private TextBox textBox1;
        private Label label1;
    }
}