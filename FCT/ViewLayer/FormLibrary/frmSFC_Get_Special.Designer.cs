namespace FCT.ViewLayer.FormLibrary
{
    partial class frmSFC_Get_Special
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.rtbInput = new System.Windows.Forms.RichTextBox();
            this.rdbGetMainBarcode = new System.Windows.Forms.RadioButton();
            this.rdbGetSubPartBarcode = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.radioButton5 = new System.Windows.Forms.RadioButton();
            this.radioButton6 = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.btnDo = new System.Windows.Forms.Button();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.rtbOutput = new System.Windows.Forms.RichTextBox();
            this.rdbGetModel = new System.Windows.Forms.RadioButton();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // rtbInput
            // 
            this.rtbInput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbInput.Location = new System.Drawing.Point(3, 21);
            this.rtbInput.Name = "rtbInput";
            this.rtbInput.Size = new System.Drawing.Size(294, 367);
            this.rtbInput.TabIndex = 0;
            this.rtbInput.Text = "";
            // 
            // rdbGetMainBarcode
            // 
            this.rdbGetMainBarcode.AutoSize = true;
            this.rdbGetMainBarcode.Location = new System.Drawing.Point(6, 34);
            this.rdbGetMainBarcode.Name = "rdbGetMainBarcode";
            this.rdbGetMainBarcode.Size = new System.Drawing.Size(125, 19);
            this.rdbGetMainBarcode.TabIndex = 1;
            this.rdbGetMainBarcode.TabStop = true;
            this.rdbGetMainBarcode.Text = "get main barcode";
            this.rdbGetMainBarcode.UseVisualStyleBackColor = true;
            // 
            // rdbGetSubPartBarcode
            // 
            this.rdbGetSubPartBarcode.AutoSize = true;
            this.rdbGetSubPartBarcode.Location = new System.Drawing.Point(6, 59);
            this.rdbGetSubPartBarcode.Name = "rdbGetSubPartBarcode";
            this.rdbGetSubPartBarcode.Size = new System.Drawing.Size(142, 19);
            this.rdbGetSubPartBarcode.TabIndex = 4;
            this.rdbGetSubPartBarcode.TabStop = true;
            this.rdbGetSubPartBarcode.Text = "get sub part barcode";
            this.rdbGetSubPartBarcode.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rdbGetModel);
            this.groupBox1.Controls.Add(this.rdbGetSubPartBarcode);
            this.groupBox1.Controls.Add(this.rdbGetMainBarcode);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(200, 121);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "get type";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.radioButton5);
            this.groupBox2.Controls.Add(this.radioButton6);
            this.groupBox2.Location = new System.Drawing.Point(218, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(300, 121);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "source data";
            // 
            // radioButton5
            // 
            this.radioButton5.AutoSize = true;
            this.radioButton5.Location = new System.Drawing.Point(6, 59);
            this.radioButton5.Name = "radioButton5";
            this.radioButton5.Size = new System.Drawing.Size(140, 19);
            this.radioButton5.TabIndex = 4;
            this.radioButton5.TabStop = true;
            this.radioButton5.Text = "main board barcode";
            this.radioButton5.UseVisualStyleBackColor = true;
            // 
            // radioButton6
            // 
            this.radioButton6.AutoSize = true;
            this.radioButton6.Location = new System.Drawing.Point(6, 34);
            this.radioButton6.Name = "radioButton6";
            this.radioButton6.Size = new System.Drawing.Size(135, 19);
            this.radioButton6.TabIndex = 1;
            this.radioButton6.TabStop = true;
            this.radioButton6.Text = "lower case barcode";
            this.radioButton6.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.radioButton1);
            this.groupBox3.Controls.Add(this.radioButton2);
            this.groupBox3.Location = new System.Drawing.Point(524, 12);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(300, 121);
            this.groupBox3.TabIndex = 7;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "target data";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.rtbInput);
            this.groupBox4.Location = new System.Drawing.Point(12, 139);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(300, 391);
            this.groupBox4.TabIndex = 8;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "input";
            // 
            // btnDo
            // 
            this.btnDo.Location = new System.Drawing.Point(346, 280);
            this.btnDo.Name = "btnDo";
            this.btnDo.Size = new System.Drawing.Size(115, 32);
            this.btnDo.TabIndex = 9;
            this.btnDo.Text = "=>";
            this.btnDo.UseVisualStyleBackColor = true;
            this.btnDo.Click += new System.EventHandler(this.btnDo_Click);
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.rtbOutput);
            this.groupBox5.Location = new System.Drawing.Point(524, 139);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(300, 391);
            this.groupBox5.TabIndex = 10;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "output";
            // 
            // rtbOutput
            // 
            this.rtbOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbOutput.Location = new System.Drawing.Point(3, 21);
            this.rtbOutput.Name = "rtbOutput";
            this.rtbOutput.Size = new System.Drawing.Size(294, 367);
            this.rtbOutput.TabIndex = 0;
            this.rtbOutput.Text = "";
            // 
            // rdbGetModel
            // 
            this.rdbGetModel.AutoSize = true;
            this.rdbGetModel.Location = new System.Drawing.Point(6, 84);
            this.rdbGetModel.Name = "rdbGetModel";
            this.rdbGetModel.Size = new System.Drawing.Size(84, 19);
            this.rdbGetModel.TabIndex = 11;
            this.rdbGetModel.TabStop = true;
            this.rdbGetModel.Text = "get model";
            this.rdbGetModel.UseVisualStyleBackColor = true;
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(6, 59);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(140, 19);
            this.radioButton1.TabIndex = 6;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "main board barcode";
            this.radioButton1.UseVisualStyleBackColor = true;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(6, 34);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(135, 19);
            this.radioButton2.TabIndex = 5;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "lower case barcode";
            this.radioButton2.UseVisualStyleBackColor = true;
            // 
            // frmSFC_Get_Special
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(848, 548);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.btnDo);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "frmSFC_Get_Special";
            this.Text = "frmSFC_Get_Special";
            this.Load += new System.EventHandler(this.frmSFC_Get_Special_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtbInput;
        private System.Windows.Forms.RadioButton rdbGetMainBarcode;
        private System.Windows.Forms.RadioButton rdbGetSubPartBarcode;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.RadioButton radioButton5;
        private System.Windows.Forms.RadioButton radioButton6;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button btnDo;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.RichTextBox rtbOutput;
        private System.Windows.Forms.RadioButton rdbGetModel;
        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton2;
    }
}