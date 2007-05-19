/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;

namespace Cat
{
    public partial class GraphWindow : Form
    {
        List<Object> mValues = new List<Object>();
        List<String> mNames = new List<String>();
        List<Function> mFxns = new List<Function>();
        Mutex mMutex = new Mutex();
        
        public GraphWindow()
        {
            InitializeComponent();
        }

        public void ClearFxns()
        {
            mMutex.WaitOne();
            mFxns.Clear();
            mMutex.ReleaseMutex();

            // Tell the parent thread to invalidate
            MethodInvoker p = Invalidate;
            Invoke(p);
        }

        public void AddFxn(Function f)
        {
            mMutex.WaitOne();
            mFxns.Add(f);
            mMutex.ReleaseMutex();

            // Tell the parent thread to invalidate
            MethodInvoker p = Invalidate;  
            Invoke(p);
        }

        private Point CatListToPoint(FList list)
        {
            return new Point((int)list.Nth(1), (int)list.Nth(0));
        }

        public void SaveToFile(string s)
        {
            mMutex.WaitOne();

            Bitmap b = new Bitmap(Width, Height, CreateGraphics());
            Drawer.Initialize(Graphics.FromImage(b));
            foreach (Function f in mFxns) {
                Drawer.draw(f);
            }

            b.Save(s);

            mMutex.ReleaseMutex();
        }

        public delegate void Proc();

        public void SafeClose()
        {
            if (InvokeRequired)
            {
                Proc p = SafeClose;
                Invoke(p, null);
            }
            else
            {
                Close();
            }
        }

        private void GraphWindow_Paint(object sender, PaintEventArgs e)
        {
            mMutex.WaitOne();

            Drawer.Initialize(e.Graphics);
            foreach (Function f in mFxns)
                Drawer.draw(f);

            Drawer.draw_turtle();

            mMutex.ReleaseMutex();
        }

        private void GraphWindow_Resize(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void GraphWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
        }
    }

    public class Drawer
    {
        static GraphWindow mWindow;
        static EventWaitHandle mWait = new EventWaitHandle(false, EventResetMode.AutoReset);
        static Executor mExec = new Executor();
        static Pen mPen = new Pen(Color.Black);
        static bool mbPenUp = false;
        static Pen turtlePen = new Pen(Color.Blue);
        static Brush turtleBrush = new SolidBrush(Color.DarkSeaGreen);
        static int mnRendering = 0;
        static Graphics mg;       

        public static void Initialize(Graphics g)
        {
            mg = g;
            Point mOrigin = new Point(0, 0);
            mPen.LineJoin = LineJoin.Bevel;
            turtlePen.Width = 1;
            mg = g;
            mg.TranslateTransform(mWindow.Width / 2, mWindow.Height / 2);
        }

        private static void private_draw(Function f)
        {
            try
            {
                Trace.Assert(mnRendering >= 0);
                mnRendering++;
                f.Eval(mExec);
            }
            finally
            {
                mnRendering--;
                Trace.Assert(mnRendering >= 0);
            }
        }

        public static void draw(Function f)
        {
            Trace.Assert(mnRendering == 0);
            try
            {
                private_draw(f);
            }
            finally
            {
                Trace.Assert(mnRendering == 0);
            }
        }

        public static void render(Function f)
        {
            // WARNING: This might be a possible race condition 
            // I have to analyze it further.
            if (mnRendering == 0)
            {
                if (mWindow == null)
                {
                    open_window();
                }
                mWindow.AddFxn(f);
                mWindow.Invalidate();
            }
            else
            {
                private_draw(f);
            }
        }

        public static void rotate(double x)
        {
            mg.RotateTransform((float)x);
        }

        public static void line_to(double x, double y)
        {
            if (!mbPenUp)
                mg.DrawLine(mPen, 0, 0, (float)x, (float)y);
            mg.TranslateTransform((float)x, (float)y);
        }

        public static void scale(double x)
        {
            mg.ScaleTransform((float)x, (float)x);
        }

        public static void set_pen_up(bool b)
        {
            mbPenUp = b;
        }

        public static bool get_pen_up()
        {
            return mbPenUp;
        }

        public static void line(double x0, double y0, double x1, double y1)
        {
            if (!mbPenUp)
                mg.DrawLine(mPen, (float)x0, (float)y0, (float)x1, (float)y1);
        }

        public static void rectangle(double x, double y, double w, double h)
        {
            if (!mbPenUp)
                mg.DrawRectangle(mPen, (float)x, (float)y, (float)w, (float)h);
        }

        public static void ellipse(double w, double h)
        {
            if (!mbPenUp)
                mg.DrawEllipse(mPen, (float)w / 2, (float)h / 2, (float)w, (float)h);
        }

        public static void draw_turtle_foot(int x, int y)
        {
            int footSize = 10;
            Rectangle rect = new Rectangle(x - (footSize / 2), y - (footSize / 2), footSize, footSize);            
            mg.FillEllipse(turtleBrush, rect);
            mg.DrawEllipse(turtlePen, rect);
        }

        public static void draw_turtle()
        {
            int w = 26; 
            int h = 26;

            // draw feet
            int x = (h / 2) - 3;
            int y = (w / 2) - 3;
            draw_turtle_foot(-x, y);
            draw_turtle_foot(-x, -y);
            draw_turtle_foot(x, y);
            draw_turtle_foot(x, -y);

            // draw head 
            draw_turtle_foot(w / 2 + 3, 0);

            // draw eyes
            Rectangle eye1 = new Rectangle(w / 2 + 4, 2, 1, 1);
            Rectangle eye2 = new Rectangle(w / 2 + 4, -3, 1, 1);
            mg.DrawRectangle(turtlePen, eye1);
            mg.DrawRectangle(turtlePen, eye2);

            // draw tail
            Rectangle rect = new Rectangle(-(w/2) - 6, -2, 12, 4);
            mg.FillEllipse(turtleBrush, rect);
            mg.DrawEllipse(turtlePen, rect);
           
            // draw body
            rect = new Rectangle(-w / 2, -h / 2, w, h);
            mg.FillEllipse(turtleBrush, rect);
            mg.DrawEllipse(turtlePen, rect);
            
            // clip everything else to the body
            GraphicsPath clipPath = new GraphicsPath();
            clipPath.AddEllipse(rect);
            mg.SetClip(clipPath);

            // stripe the body
            turtlePen.Width = 2;
            mg.DrawLine(turtlePen, 0, -h/2, 0, h/2);

            int curvature = w / 2;
            mg.DrawEllipse(turtlePen, 7, -h/2, curvature, h);
            mg.DrawEllipse(turtlePen, -(curvature + 7), -h / 2, curvature, h);
            mg.ResetClip();
        }

        public static void pen_color(Color x)
        {
            mPen.Color = x;
        }

        public static void pen_width(int x)
        {
            mPen.Width = x;
        }

        public static void set_solid_fill(Color x)
        {
            mPen.Brush = new SolidBrush(x);
        }

        public static void no_fill()
        {
            mPen.Brush = null;
        }

        public static void polygon(FList x)
        {
            mg.DrawPolygon(mPen, ListToPointArray(x));
        }

        public static void lines(FList x)
        {
            mg.DrawLines(mPen, ListToPointArray(x));
        }

        #region color functions
        static public Color blue() { return Color.Blue; }
        static public Color red() { return Color.Red; }
        static public Color green() { return Color.Green; }
        static public Color rgb(int r, int g, int b) { return Color.FromArgb(r, g, b); }
        #endregion

        #region helper functions
        private static Point ListToPoint(FList x)
        {
            return new Point((int)x.Nth(1), (int)x.Nth(0));
        }

        private static Point[] ListToPointArray(FList x)
        {
            Point[] result = new Point[x.Count()];
            int i = 0;
            x.ForEach(delegate(Object o)
            {
                FList tmp = o as FList;
                result[i++] = ListToPoint(tmp);
            });
            return result;
        }
        #endregion

        #region public functions
        static public void open_window()
        {
            if (mWindow != null) return;
            Thread t = new Thread(new ThreadStart(LaunchWindow));
            t.Start();
            mWait.WaitOne();   
        }

        static public void close_window()
        {
            if (mWindow == null) return;
            mWindow.SafeClose();
            mWindow = null;
        }

        static public void clear_screen()
        {
            mWindow.ClearFxns();
        }

        static public void save_window(string s)
        {
            mWindow.SaveToFile(s);
        }
        #endregion 


        static private Form GetForm()
        {
            return mWindow;
        }

        static private void LaunchWindow()
        {
            mWindow = new GraphWindow();
            mWait.Set();
            Application.Run(mWindow);
        }
    }
}