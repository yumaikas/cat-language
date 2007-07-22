using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Reflection;

namespace Cat
{
    partial class EditDefForm : Form
    {
        #region public fields
        public Function result;
        #endregion 

        #region constructors
        public EditDefForm()
        {
            InitializeComponent();
            Output.SetCallBack(OnWrite);
        }
        #endregion 

        #region static data
        static EditDefForm form = new EditDefForm();
        #endregion 

        #region static functions
        public static Function EditFunction(Function f)
        {
            form.Init(f);
            DialogResult result = form.ShowDialog();

            if (result != DialogResult.OK)
                return null;

            return form.result;
        }

        public static Function DefineNewFunction()
        {
            form.Init();
            DialogResult result = form.ShowDialog();

            if (result != DialogResult.OK)
                return null;

            return form.result;
        }
        #endregion


        void OnWrite(string s)
        {
            if (Visible)
                messages.AppendText(s);
        }

        public void Init(Function f)
        {
            result = null;
            textBoxName.Text = f.GetName();
            textBoxMetadata.Text = f.GetMetaDataString();
            textBoxType.Text = f.GetFxnTypeString();
            textBoxImpl.Text = f.GetImpl();
        }

        public void Init()
        {
            result = null;
            textBoxName.Clear();
            textBoxMetadata.Clear();
            textBoxType.Clear();
            textBoxImpl.Clear();
        }

        public Context GetContext()
        {
            return Executor.Main.GetGlobalContext();
        }

        public void SetWarningState(TextBoxBase ctrl)
        {
            ctrl.BackColor = Color.FromArgb(255, 128, 128);
        }

        public void ClearWarningState(TextBoxBase ctrl)
        {
            ctrl.BackColor = Color.FromKnownColor(KnownColor.Window);
        }

        private void Log(string s)
        {
            messages.AppendText(s + "\n");
        }

        public CatFxnType GetFxnType()
        {
            try
            {
                string s = textBoxType.Text.Trim();
                if (s.Length == 0)
                    return null;
                CatFxnType ret = CatFxnType.Create(s);
                ClearWarningState(textBoxType);
                return ret;
            }
            catch (Exception e)
            {
                Log("error parsing function type");
                Log(e.Message);
                SetWarningState(textBoxType);
                return null;
            }
        }

        public CatMetaDataBlock GetMetaData()
        {
            try
            {
                string s = textBoxMetadata.Text.Trim();
                if (s.Length == 0)
                    return null;
                CatMetaDataBlock ret = CatMetaDataBlock.Create(s);
                ClearWarningState(textBoxMetadata);
                return ret;
            }
            catch (Exception e)
            {
                Log("error parsing meta-data");
                Log(e.Message);
                SetWarningState(textBoxMetadata);
                return null;
            }
        }

        public List<Function> GetImpl(DefinedFunction def)
        {
            try
            {
                string s = textBoxImpl.Text.Trim();
                List<AstExprNode> impl = CatParser.ParseExpr(s);
                List<Function> ret = CatParser.TermsToFxns(impl, def);
                ClearWarningState(textBoxImpl);
                return ret;
            }
            catch (Exception e)
            {
                Log("error processing implementation");
                Log(e.Message);
                SetWarningState(textBoxMetadata);
                return null;
            }
        }

        public Function ConstructFunction()
        {
            try
            {
                messages.Clear();
                string sName = textBoxName.Text.Trim();
                if (sName.Length == 0)
                {
                    Log("unnamed function");
                    return null;
                }
                Log("constructing function: " + sName);
                if (GetContext().FunctionExists(sName))
                    Log("warning: redefining " + sName);

                CatFxnType ft = GetFxnType();
                CatMetaData md = GetMetaData();
                DefinedFunction def = new DefinedFunction(sName);
                List<Function> fxns = GetImpl(def);
                if (fxns == null)
                {
                    Log("unable to construct function");
                    return null;
                }
                def.AddFunctions(fxns);
                def.RunTests();
                return def;
            }
            catch (Exception e)
            {
                Log("unable to construct function");
                Log(e.Message);
                return null;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private void textBoxName_Leave(object sender, EventArgs e)
        {
            ConstructFunction();
        }

        private void textBoxType_Leave(object sender, EventArgs e)
        {
            ConstructFunction();
        }

        private void textBoxMetadata_Leave(object sender, EventArgs e)
        {
            ConstructFunction();
        }

        private void textBoxImpl_Leave(object sender, EventArgs e)
        {
            ConstructFunction();
        }

        private void EditDefForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                Function f = ConstructFunction();
                if (f == null)
                {
                    MessageBox.Show("An error was encountered during function construction, please fix the error, or press cancel");
                    e.Cancel = true;
                }
            }
        }

        private void EditDefForm_Shown(object sender, EventArgs e)
        {
            Focus();
        }
    }
}
