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

        private Point CatListToPoint(CatList l)
        {
            return new Point((int)l.nth(1), (int)l.nth(0));
        }

        public void SaveToFile(string s)
        {
            mMutex.WaitOne();

            Bitmap b = new Bitmap(Width, Height, CreateGraphics());
            gdi g = new gdi(this, Graphics.FromImage(b));
            foreach (Function f in mFxns) 
                g.render(f);

            b.Save(s);

            mMutex.ReleaseMutex();
        }

        private void GraphWindow_Paint(object sender, PaintEventArgs e)
        {
            mMutex.WaitOne();

            gdi g = new gdi(this, e.Graphics);
            foreach (Function f in mFxns)
                g.render(f);            

            mMutex.ReleaseMutex();
        }

        private void GraphWindow_Resize(object sender, EventArgs e)
        {
            Invalidate();
        }
    }

    public class gdi
    {
        public Executor mExec = new Executor();

        Pen mPen = new Pen(Color.Black);
        bool mbPenUp = false;

        GraphWindow mw;
        Graphics mg;
        
        public gdi(GraphWindow w, Graphics g)
        {
            Point mOrigin = new Point(0, 0);
            mPen.LineJoin = LineJoin.Bevel;
            mw = w;
            mg = g;
            mg.TranslateTransform(mw.Width / 2, mw.Height / 2);
        }

        public void render(Function f)
        {
            mExec.Push(this);
            f.Eval(mExec.GetStack());
            mExec.Pop();
        }

        public void rotate(int x)
        {
            mg.RotateTransform(x);
        }

        public void line_to(int x, int y)
        {
            if (!mbPenUp)
                mg.DrawLine(mPen, 0, 0, x, y);
            mg.TranslateTransform(x, y);
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

        public void line(int x0, int y0, int x1, int y1)
        {
            if (!mbPenUp)
                mg.DrawLine(mPen, x0, y0, x1, y1);
        }

        public void rectangle(int x, int y, int w, int h)
        {
            if (!mbPenUp)
                mg.DrawRectangle(mPen, x, y, w, h);
        }

        public void ellipse(int x, int y, int w, int h)
        {
            if (!mbPenUp)
                mg.DrawEllipse(mPen, x, y, w, h);
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

        public void poly(CatList x)
        {
            mg.DrawPolygon(mPen, ListToPointArray(x));
        }
        
        public void lines(CatList x)
        {
            mg.DrawLines(mPen, ListToPointArray(x));
        }

        #region helper functions
        private Point ListToPoint(CatList x)
        {
            return new Point((int)x.nth(1), (int)x.nth(0));
        }

        private Point[] ListToPointArray(CatList x)
        {
            Point[] result = new Point[x.count()];
            int i = 0;
            Accessor acc = delegate(Object o)
            {
                CatList tmp = o as CatList;
                result[i++] = ListToPoint(tmp);
            };
            x.WithEach(acc);
            return result;
        }
        #endregion
    }

    public class window 
    {
        GraphWindow mWindow;
        EventWaitHandle mWait = new EventWaitHandle(false, EventResetMode.AutoReset);

        public window()
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

        public void render(Function f)
        {
            mWindow.AddFxn(f);
        }

        public void clear_screen()
        {
            mWindow.ClearFxns();
        }

        public void save(string s)
        {
            mWindow.SaveToFile(s);
        }

        static public Color blue() { return Color.Blue; }
        static public Color red() { return Color.Red; }
        static public Color green() { return Color.Green; }
        static public Color rgb(int r, int g, int b) { return Color.FromArgb(r, g, b); }
    }
}