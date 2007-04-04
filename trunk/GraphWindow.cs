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
        List<Object> mValues = new List<Object>();
        List<String> mNames = new List<String>();

        Mutex mMutex = new Mutex();

        public GraphWindow()
        {
            InitializeComponent();
        }
        
        public void SetProp(string s, Object o)
        {
            mMutex.WaitOne();

            mValues.Add(o);
            mNames.Add(s);

            mMutex.ReleaseMutex();

            // Tell the parent thread to invalidate
            MethodInvoker p = Refresh;  
            Invoke(p);
        }

        private Point CatListToPoint(CatList l)
        {
            return new Point((int)l.nth(1), (int)l.nth(0));
        }

        private void GraphWindow_Paint(object sender, PaintEventArgs e)
        {
            mMutex.WaitOne();
            Pen pen = new System.Drawing.Pen(System.Drawing.Color.Black);
            Point origin = new Point(0, 0);
            Point cur;
            bool bPenUp = false;

            for (int i = 0; i < mNames.Count; ++i )
            {
                string s = mNames[i];
                Object val = mValues[i];

                switch (s)
                {
                    case "set_text":
                        Text = val as String;
                        break;
                    case "rotate":
                        p.Graphics.RotateTransform((float)val);
                        break;
                    case "pen_color":
                        pen.Color = (Color)val;
                        break;
                    case "pen_width":
                        pen.Width = (int)val;
                        break;
                    case "line_to":
                        cur = CatListToPoint(val as CatList);
                        if (!bPenUp)
                            e.Graphics.DrawLine(pen, origin, cur);
                        break;
                    case "line_rel":
                        prev = cur;
                        cur = CatListToPoint(val as CatList);
                        cur.X = prev.X + cur.X;
                        cur.Y = prev.Y + cur.Y;
                        if (!bPenUp)
                            e.Graphics.DrawLine(pen, prev, cur);
                        break;
                    case "pen_up":
                        bPenUp = (bool)val;
                        break;
                    case "bg":
                        BackColor = (Color)val;
                        break;
                }
            }

            mMutex.ReleaseMutex();
        }
    }

    public class wnd 
    {
        GraphWindow mWindow;
        EventWaitHandle mWait = new EventWaitHandle(false, EventResetMode.AutoReset);

        public wnd()
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

        public void set(Object o, String s)
        {
            mWindow.SetProp(s, o);
        }

        public void move_to(CatList x)
        {
            set(x, "move_to");
        }

        public void line_to(CatList x)
        {
            set(x, "line_to");
        }

        public void move_rel(CatList x)
        {
            set(x, "move_rel");
        }

        public void line_rel(CatList x)
        {
            set(x, "line_rel");
        }

        public void pen_color(Color x)
        {
            set(x, "pen_color");
        }

        public void pen_color(int x)
        {
            set(x, "pen_width");
        }

        public void rotate(double x)
        {
            set((float)x, "rotate");
        }

        public void rotate(int x)
        {
            set((float)x, "rotate");
        }

        public void pen_up(bool x)
        {
            set(x, "pen_up");
        }

        static public Color blue() { return Color.Blue; }
        static public Color red() { return Color.Red; }
        static public Color green() { return Color.Green; }
        static public Color rgb(int r, int g, int b) { return Color.FromArgb(r, g, b); }
    }
}