/// Public domain code by Christopher Diggins
/// http://www.cat-language.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace Cat
{
    public partial class GraphWindow : Form
    {
        List<Object> mValues = new List<Object>();
        List<String> mNames = new List<String>();
        List<Function> mFxns = new List<Function>();
        Mutex mMutex = new Mutex();
        
        Executor mExec;
        Drawer mDrawer;

        public GraphWindow()
        {
            InitializeComponent();
            // Clone the main executor
            mExec = new Executor(Executor.Main);
            // create a drawing object that uses this executor
            mDrawer = new Drawer(mExec);
            // register the drawing object with the executor
            Scope scope = mExec.GetGlobalScope();
            
            // TODO: Problem: a dummy drawer object was already created.
            // options, remove or replace all functions. 
            // or have a single global drawer object that delegates to the 
            // correct place. The "drawer" object could be static.
            // Another problem, at least it used to be, was the inability
            // to define new functions and use them. 
            // Perhaps two executors is not the correct thing to do? 
            // there is lots of global data. 
            scope.RegisterObject(mDrawer);
            
            // It is important that we override the render function.
            // otherwise we are left with two conflicting methods 
            // internally: the window manager version, and the drawer version.
            // the window manager version will cause serious problems 
            scope.RemoveFunctions("render");
            scope.AddMethod(mDrawer, mDrawer.GetType().GetMethod("render"));
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
            mDrawer.Initialize(Graphics.FromImage(b), this);
            foreach (Function f in mFxns) 
                mDrawer.render(f);

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

            mDrawer.Initialize(e.Graphics, this);
            foreach (Function f in mFxns)
                mDrawer.render(f);

            mDrawer.draw_turtle();

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
        Executor mExec;
        Pen mPen = new Pen(Color.Black);
        bool mbPenUp = false;
        Pen turtlePen = new Pen(Color.Blue);
        Brush turtleBrush = new SolidBrush(Color.DarkSeaGreen);

        GraphWindow mw;
        Graphics mg;
        
        public Drawer(Executor exec)
        {
            mExec = exec;
        }

        // not intended for the executor to see it, but no way to hide it.
        public void Initialize(Graphics g, GraphWindow w)
        {
            mg = g;
            Point mOrigin = new Point(0, 0);
            mPen.LineJoin = LineJoin.Bevel;
            turtlePen.Width = 1;
            mw = w;
            mg = g;
            mg.TranslateTransform(mw.Width / 2, mw.Height / 2);
        }

        public void render(Function f)
        {
            // Routes all graphics calls to this object.
            mExec.GetGlobalScope().RegisterObject(this);
            f.Eval(mExec);
        }

        public void rotate(double x)
        {
            mg.RotateTransform((float)x);
        }

        public void line_to(double x, double y)
        {
            if (!mbPenUp)
                mg.DrawLine(mPen, 0, 0, (float)x, (float)y);
            mg.TranslateTransform((float)x, (float)y);
        }

        public void scale(double x)
        {
            mg.ScaleTransform((float)x, (float)x);
        }

        public void set_pen_up(bool b)
        {
            mbPenUp = b;
        }

        public bool get_pen_up()
        {
            return mbPenUp;
        }

        public void line(double x0, double y0, double x1, double y1)
        {
            if (!mbPenUp)
                mg.DrawLine(mPen, (float)x0, (float)y0, (float)x1, (float)y1);
        }

        public void rectangle(double x, double y, double w, double h)
        {
            if (!mbPenUp)
                mg.DrawRectangle(mPen, (float)x, (float)y, (float)w, (float)h);
        }

        public void ellipse(double w, double h)
        {
            if (!mbPenUp)
                mg.DrawEllipse(mPen, (float)w / 2, (float)h / 2, (float)w, (float)h);
        }

        public void draw_turtle_foot(int x, int y)
        {
            int footSize = 10;
            Rectangle rect = new Rectangle(x - (footSize / 2), y - (footSize / 2), footSize, footSize);            
            mg.FillEllipse(turtleBrush, rect);
            mg.DrawEllipse(turtlePen, rect);
        }

        public void draw_turtle()
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

        public void pen_color(Color x)
        {
            mPen.Color = x;
        }

        public void pen_width(int x)
        {
            mPen.Width = x;
        }

        public void set_solid_fill(Color x)
        {
            mPen.Brush = new SolidBrush(x);
        }

        public void no_fill()
        {
            mPen.Brush = null;
        }

        public void polygon(FList x)
        {
            mg.DrawPolygon(mPen, ListToPointArray(x));
        }

        public void lines(FList x)
        {
            mg.DrawLines(mPen, ListToPointArray(x));
        }

        #region helper functions
        private Point ListToPoint(FList x)
        {
            return new Point((int)x.Nth(1), (int)x.Nth(0));
        }

        private Point[] ListToPointArray(FList x)
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
    }

    static public class WindowManager
    {
        static GraphWindow mWindow;
        static EventWaitHandle mWait = new EventWaitHandle(false, EventResetMode.AutoReset);

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

        /*
        static public void render(Function f)
        {
            mWindow.AddFxn(f);
        }
         */
        #endregion 

        #region color functions
        static public Color blue() { return Color.Blue; }
        static public Color red() { return Color.Red; }
        static public Color green() { return Color.Green; }
        static public Color rgb(int r, int g, int b) { return Color.FromArgb(r, g, b); }
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