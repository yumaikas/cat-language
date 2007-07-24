namespace Cat
{
    partial class CodeViewForm
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
            this.components = new System.ComponentModel.Container();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.mEdit = new System.Windows.Forms.RichTextBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ShowCommentsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ShowImplMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ShowInferredTypeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ShowCondensedMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.EditDefMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.NewDefMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(0, 325);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(418, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // mEdit
            // 
            this.mEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.mEdit.ContextMenuStrip = this.contextMenuStrip1;
            this.mEdit.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.mEdit.Location = new System.Drawing.Point(0, -2);
            this.mEdit.Name = "mEdit";
            this.mEdit.Size = new System.Drawing.Size(418, 324);
            this.mEdit.TabIndex = 0;
            this.mEdit.Text = "";
            this.mEdit.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.mEdit_MouseDoubleClick);
            this.mEdit.MouseClick += new System.Windows.Forms.MouseEventHandler(this.mEdit_MouseClick);
            this.mEdit.MouseUp += new System.Windows.Forms.MouseEventHandler(this.mEdit_MouseUp);
            this.mEdit.MouseMove += new System.Windows.Forms.MouseEventHandler(this.mEdit_MouseMove);
            this.mEdit.VScroll += new System.EventHandler(this.mEdit_VScroll);
            this.mEdit.TextChanged += new System.EventHandler(this.mEdit_TextChanged);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ShowCommentsMenuItem,
            this.ShowImplMenuItem,
            this.ShowInferredTypeMenuItem,
            this.ShowCondensedMenuItem,
            this.EditDefMenuItem,
            this.NewDefMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(178, 136);
            this.contextMenuStrip1.Closed += new System.Windows.Forms.ToolStripDropDownClosedEventHandler(this.contextMenuStrip1_Closed);
            // 
            // ShowCommentsMenuItem
            // 
            this.ShowCommentsMenuItem.Checked = true;
            this.ShowCommentsMenuItem.CheckOnClick = true;
            this.ShowCommentsMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowCommentsMenuItem.Name = "ShowCommentsMenuItem";
            this.ShowCommentsMenuItem.Size = new System.Drawing.Size(177, 22);
            this.ShowCommentsMenuItem.Text = "Show Comments";
            this.ShowCommentsMenuItem.Click += new System.EventHandler(this.ShowCommentsMenuItem_Click);
            // 
            // ShowImplMenuItem
            // 
            this.ShowImplMenuItem.Checked = true;
            this.ShowImplMenuItem.CheckOnClick = true;
            this.ShowImplMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowImplMenuItem.Name = "ShowImplMenuItem";
            this.ShowImplMenuItem.Size = new System.Drawing.Size(177, 22);
            this.ShowImplMenuItem.Text = "Show Implementation";
            this.ShowImplMenuItem.Click += new System.EventHandler(this.ShowImplMenuItem_Click);
            // 
            // ShowInferredTypeMenuItem
            // 
            this.ShowInferredTypeMenuItem.Checked = true;
            this.ShowInferredTypeMenuItem.CheckOnClick = true;
            this.ShowInferredTypeMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.ShowInferredTypeMenuItem.Name = "ShowInferredTypeMenuItem";
            this.ShowInferredTypeMenuItem.Size = new System.Drawing.Size(177, 22);
            this.ShowInferredTypeMenuItem.Text = "Show Inferred Types";
            this.ShowInferredTypeMenuItem.Click += new System.EventHandler(this.ShowInferredTypeMenuItem_Click);
            // 
            // ShowCondensedMenuItem
            // 
            this.ShowCondensedMenuItem.CheckOnClick = true;
            this.ShowCondensedMenuItem.Name = "ShowCondensedMenuItem";
            this.ShowCondensedMenuItem.Size = new System.Drawing.Size(177, 22);
            this.ShowCondensedMenuItem.Text = "Show Condensed";
            this.ShowCondensedMenuItem.Click += new System.EventHandler(this.ShowCondensedMenuItem_Click);
            // 
            // EditDefMenuItem
            // 
            this.EditDefMenuItem.Name = "EditDefMenuItem";
            this.EditDefMenuItem.Size = new System.Drawing.Size(177, 22);
            this.EditDefMenuItem.Text = "Edit Definition";
            this.EditDefMenuItem.Click += new System.EventHandler(this.EditDefMenuItem_Click);
            // 
            // NewDefMenuItem
            // 
            this.NewDefMenuItem.Name = "NewDefMenuItem";
            this.NewDefMenuItem.Size = new System.Drawing.Size(177, 22);
            this.NewDefMenuItem.Text = "New Defintion";
            // 
            // CodeViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(418, 347);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.mEdit);
            this.Name = "CodeViewForm";
            this.Text = "Cat Code Viewer";
            this.Shown += new System.EventHandler(this.CodeViewForm_Shown);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.RichTextBox mEdit;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem ShowCommentsMenuItem;
        private System.Windows.Forms.ToolStripMenuItem NewDefMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ShowImplMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ShowInferredTypeMenuItem;
        private System.Windows.Forms.ToolStripMenuItem ShowCondensedMenuItem;
        private System.Windows.Forms.ToolStripMenuItem EditDefMenuItem;
    }
}