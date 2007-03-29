using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace Cat
{
    public partial class GraphWindow : Form
    {
        Dictionary<string, Object> mProps = new Dictionary<string,object>();
        Mutex mMutex = new Mutex();

        public GraphWindow()
        {
            InitializeComponent();
        }
        
        public void SetProp(string s, Object o)
        {
            mMutex.WaitOne();

            mProps[s] = o;

            mMutex.ReleaseMutex();

            // Tell the parent thread to invalidate
            MethodInvoker p = Refresh;
            Invoke(p);
        }

        private void GraphWindow_Paint(object sender, PaintEventArgs e)
        {
            mMutex.WaitOne();

            foreach (string s in mProps.Keys)
            {
                switch (s)
                {
                    case "text":
                        Text = mProps[s] as String;
                        break;
                    case "bg":
                        BackColor = (Color)mProps[s];
                        break;
                }
            }

            mMutex.ReleaseMutex();
        }
    }

    public class Window 
    {
        GraphWindow mWindow;
        EventWaitHandle mWait = new EventWaitHandle(false, EventResetMode.AutoReset);

        public Window()
        {            
            Thread t = new Thread(new ThreadStart(LaunchWindow));
            t.Start();
            mWait.WaitOne();   
        }

        private Form GetForm()
        {
            return mWindow;
        }

        private void LaunchWindow()
        {
            mWindow = new GraphWindow();
            mWait.Set();
            Application.Run(mWindow);
        }

        public void set_text(string s)
        {
            mWindow.SetProp("text", s);
        }

        public void set_bg_color(Color c)
        {
            mWindow.SetProp("bg", c);
        }
        
        static public Color blue() { return Color.Blue; }
        static public Color red() { return Color.Red; }
        static public Color green() { return Color.Green; }
        static public Color rgb(int r, int g, int b) { return Color.FromArgb(r, g, b); }
    }

    public class Vector2D
    {
        // [1 2] draw_to
        // Can I say lists and functions are the same? 
        // I could say that whereever a list is hoped for, but a function is given ... then 
        // that function is executed and the result is passed. 
        // { 1 2 } function
        // [1 2] list: 1 -> 1 [dup] 
        // [1 2 3] == list of numbers 
        // [1 2 pop] == list of functions
        // (1 2 3) == list of values 
        //public Vector2D(
        //
        // TODO: finish.
    }
}