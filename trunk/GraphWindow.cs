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

            g.draw_turtle();

            mMutex.ReleaseMutex();
        }

        private void GraphWindow_Resize(object sender, EventArgs e)
        {
            Invalidate();
        }

        private void GraphWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Note: this doesn't like it can ever be called.
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MessageBox.Show("Pop the window from the stack to close it");
            }
        }
    }

    public class gdi
    {
        public Executor mExec = new Executor();

        Pen mPen = new Pen(Color.Black);
        bool mbPenUp = false;
        Pen turtlePen = new Pen(Color.Blue);
        Brush turtleBrush = new SolidBrush(Color.DarkSeaGreen);

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
            // Routes all graphics calls to this object.
            mExec.GetGlobalScope().RegisterObject(this);
            f.Eval(mExec);
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

        public void ellipse(int w, int h)
        {
            if (!mbPenUp)
                mg.DrawEllipse(mPen, w/2, h/2, w, h);
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
            int w = 32; 
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

    static public class WindowManager
    {
        static GraphWindow mWindow;
        static EventWaitHandle mWait = new EventWaitHandle(false, EventResetMode.AutoReset);

        #region public functions
        static public open_window()
        {            
            if (mWindow != null)
                throw new Exception("window is already open");
            Thread t = new Thread(new ThreadStart(LaunchWindow));
            t.Start();
            mWait.WaitOne();   
        }

        static public void close_window()
        {
            mWindow.SafeClose();
        }

        static public void clear_window()
        {
            mWindow.ClearFxns();
        }

        static public void save_window(string s)
        {
            mWindow.SaveToFile(s);
        }

        static public void render(Function f)
        {
            mWindow.AddFxn(f);
        }
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