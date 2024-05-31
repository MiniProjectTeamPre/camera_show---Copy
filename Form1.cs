using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using ZXing;
using System.IO;
using System.Diagnostics;
using Emgu.CV.CvEnum;
using System.Collections.Generic;
using Emgu.CV.Util;
using Microsoft.VisualBasic.Devices;
using System.Linq;
using System.Threading;
using System.Reflection;
using camera_show.MiniClass;

namespace camera_show {
    public partial class Form1 : Form {
        public string head = "1";
        public string steptest;
        private int process_value = 170;
        private bool flag_process = false;
        private VideoCapture capture;
        private Image<Bgr, Byte> img;
        private static Rectangle rect;
        public Stopwatch timeout = new Stopwatch();
        private Stopwatch timeout_show = new Stopwatch();
        private int time_out = 10000;
        private bool debug = true;
        private bool flag_set_camera = false;
        private bool steptest_fail = false;
        private int crop = 30;
        private bool steptest_camera_read2d_flag = false;
        private bool steptest_camera_matching_lcd_oo_oe_eo_ee = false;
        private bool steptest_camera_check_led_red_green = false;
        private Bgr bgr_low;
        private Bgr bgr_high;
        private Hsv hsv_low;
        private Hsv hsv_high;
        private bool flag_hsv = false;
        private Image<Hsv, Byte> img_hsv;
        private bool flag_hsv_test = false;
        private int hsv_mask = 10;
        private int hsv_timeout = 100;
        private Stopwatch stopwatch_hsv_timeout = new Stopwatch();
        private bool flag_result = false;
        private string result_blackup = "";
        private string flag_set_port = "";
        private bool flag_add_step = false;
        private bool flagAutoFocus = true;
        private VideoCapture.API captureApi = VideoCapture.API.Any;
        private int digitSn = 13;
        private int degreeSet = 0;
        /// <summary>
        /// For define select address of config gamma and gain and saturation
        /// </summary>
        private string address = string.Empty;

        private EditValueCamDiv3 editValueCamDiv3;

        public Form1() {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            editValueCamDiv3 = new EditValueCamDiv3(this);
            head = File.ReadAllText("../../config/head.txt");
            File.WriteAllText("call_exe_tric.txt", "");
            try { flag_set_port = File.ReadAllText("set_port.txt"); } catch { }
            try { flag_add_step = Convert.ToBoolean(File.ReadAllText("add_step.txt")); } catch { }
            File.Delete("add_step.txt");
            try { 
                steptest = File.ReadAllText("../../config/test_head_" + head + "_steptest.txt");
            } catch { }
            try{
                debug = Convert.ToBoolean(File.ReadAllText("../../config/test_head_" + head + "_debug.txt")); 
            } catch { }
            if (steptest.Contains("read2d")) {
                steptest_camera_read2d_flag = true;
            }
            ReadFileAddress();
            ComputerInfo computerInfo = new ComputerInfo();
            if (!computerInfo.OSFullName.Contains("Windows 7")) captureApi = VideoCapture.API.DShow;
            if (flag_set_port == "set port") {
                File.Delete("set_port.txt");
                head = File.ReadAllText("camera_head_set_port.txt");
                Form f2 = new Form();
                f2.Size = new Size(100, 100);
                ComboBox c = new ComboBox();
                c.Size = new Size(60, 7);
                for (int i = 0; i < 9; i++) {
                    OpenCamera(i);
                    if (capture.Width != 0) c.Items.Add(i);
                    capture.Dispose();
                }
                f2.Controls.Add(c);
                f2.ShowDialog();
                bool flag_onebyone_check = false;
                try { flag_onebyone_check = Convert.ToBoolean(File.ReadAllText("../../config/camera_show_onebyone" + ".txt")); } catch { }
                if (flag_onebyone_check) {
                    File.WriteAllText("../../config/test_head_" + head + "_port.txt", c.Text);
                    File.WriteAllText("../../config/test_head_" + head + "_port_read2d.txt", c.Text);
                } else {
                    if (steptest_camera_read2d_flag == true) File.WriteAllText("../../config/test_head_" + head + "_port_read2d.txt", c.Text);
                    else File.WriteAllText("../../config/test_head_" + head + "_port.txt", c.Text);
                }
                OpenCamera(Convert.ToInt32(c.Text));
            } else {
                bool flag_while = true;
                if (steptest_camera_read2d_flag == true) {
                    try { string ppp = File.ReadAllText("../../config/test_head_" + head + "_port_read2d.txt"); } catch (Exception) { flag_while = false; }
                } else {
                    try { string ppp = File.ReadAllText("../../config/test_head_" + head + "_port.txt"); } catch (Exception) { flag_while = false; }
                }
                if (flag_while == false) { MessageBox.Show("_กรุณาเลือก port camera"); OpenCamera(0); }
                show_form_cancel_camera();
                Stopwatch timeout_opencam = new Stopwatch();
                timeout_opencam.Restart();
                while (flag_while) {
                    //if (timeout_opencam.ElapsedMilliseconds > 5000) { OpenCamera(0); break; }
                    if (f_cancel_camera.IsDisposed) { Application.Exit(); return; }
                    try {
                        string ff = File.ReadAllText("camera_show_list.txt");
                        if (ff == "") break;
                        if (ff.Substring(0, 1) != head) { Thread.Sleep(50); DelaymS(50); continue; }
                    } catch { Thread.Sleep(50); DelaymS(50); continue; }
                    break;
                }
                f_cancel_camera.Text = "[" + head + "]." + steptest;
                while (flag_while) {
                    //if (timeout_opencam.ElapsedMilliseconds > 5000) { OpenCamera(0); break; }
                    if (f_cancel_camera.IsDisposed) { Application.Exit(); return; }
                    try {
                        if (steptest_camera_read2d_flag == true)
                            OpenCamera(Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_port_read2d.txt")));
                        else
                            OpenCamera(Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_port.txt")));
                        if (address != Address.port)
                        {
                            int gammaCsv = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_gamma_" + steptest + ".txt"));
                            if (!CheckAddressCamera(gammaCsv))
                            {
                                if (!ScanAddressCamera(gammaCsv))
                                {
                                    //ScanAddressCamera(gammaCsv);
                                    if (!debug)
                                    {
                                        File.WriteAllText("test_head_" + head + "_result.txt", "Can not find port\r\nFAIL");
                                        this.Close();
                                        Application.Exit();
                                        Application.ExitThread();
                                        Environment.Exit(0);
                                        return;
                                    }
                                }
                            }
                        }
                        if (capture == null || capture.Ptr == IntPtr.Zero || capture.Width == 0) { DelaymS(50); Thread.Sleep(50); continue; }
                        break;
                    } catch { DelaymS(50); }
                    DelaymS(50);
                    Thread.Sleep(200);
                }
                timeout_opencam.Stop();
                f_cancel_camera.Close();
            }
            if (steptest.Contains("compar_image") || steptest.Contains("image_compar")) {
                steptest_camera_matching_lcd_oo_oe_eo_ee = true;
            }
            if (steptest.Contains("check_led")) {
                steptest_camera_check_led_red_green = true;
            }
            if (steptest_camera_read2d_flag == true) {
                Application.Idle += read2d;
            } else if (steptest_camera_matching_lcd_oo_oe_eo_ee == true) {
                Application.Idle += compar_image;
            } else if (steptest_camera_check_led_red_green == true) {
                Application.Idle += check_led;
            } else {
                MessageBox.Show("steptest.txt error : สเต็ปเทสที่ส่งเข้ามาในไฟล์ ไม่ตรงกับ ในตัวโปรแกรม camera.exe");
                File.WriteAllText("test_head_" + head + "_result.txt", "function\r\nFail");
                steptest_fail = true;
            }
            setup();
        }
        private Form f_cancel_camera = new Form();
        private void show_form_cancel_camera() {
            f_cancel_camera.Icon = Properties.Resources.icon;
            f_cancel_camera.Size = new Size(200, 70);
            f_cancel_camera.ControlBox = false;
            f_cancel_camera.Text = head + "." + steptest;
            Button b = new Button();
            b.Click += B_Click;
            b.Size = new Size(75, 30);
            b.Location = new Point(0, 0);
            b.Text = "cancel";
            Button p = new Button();
            p.Click += P_Click;
            p.Size = new Size(75, 30);
            p.Location = new Point(80, 0);
            p.Text = "set port";
            f_cancel_camera.Controls.Add(b);
            f_cancel_camera.Controls.Add(p);
            f_cancel_camera.Show();
            //f_cancel_camera.Location = new Point(0, 0);
        }
        private void P_Click(object sender, EventArgs e) {
            File.WriteAllText("set_port.txt", "set port");
            File.WriteAllText("camera_head_set_port.txt", head);
            Application.Restart();
        }
        private void B_Click(object sender, EventArgs e) {
            steptest_fail = true;
            f_cancel_camera.Close();
        }

        private void LogSendError(string value, string capProp) {
            string path = "D:\\LogError\\CameraSetCapProp";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            DateTime now = DateTime.Now;
            StreamWriter swOut = new StreamWriter(path + "\\" + now.Year + "_" + now.Month + ".csv", true);
            string time = now.Day.ToString("00") + ":" + now.Hour.ToString("00") + ":" + now.Minute.ToString("00") + ":" + now.Second.ToString("00");
            swOut.WriteLine(time + ",Value=" + value + ",Head=" + head + ",Step=" + steptest + "," + capProp);
            swOut.Close();
        }
        public void SetCapture(CapProp capProp, double value) {
            if (value == -1)
            {
                return;
            }
            if (!capture.SetCaptureProperty(capProp, value))
            {
                LogSendError(value.ToString(), capProp.ToString());
            }
        }
        private void setup() {
            if (flag_add_step) {
                try { Frame_height.Text = File.ReadAllText("../../config/test_head_" + head + "_frame_height_" + steptest.Substring(0, steptest.Length - 1) + ".txt"); } catch { }
                try { Frame_width.Text = File.ReadAllText("../../config/test_head_" + head + "_frame_width_" + steptest.Substring(0, steptest.Length - 1) + ".txt"); } catch { }
            } else {
                try { Frame_height.Text = File.ReadAllText("../../config/test_head_" + head + "_frame_height_" + steptest + ".txt"); } catch { }
                try { Frame_width.Text = File.ReadAllText("../../config/test_head_" + head + "_frame_width_" + steptest + ".txt"); } catch { }
            }
            try {
                SetCapture(CapProp.FrameHeight, Convert.ToInt32(Frame_height.Text));//800
                SetCapture(CapProp.FrameWidth, Convert.ToInt32(Frame_width.Text));//600
                SetCapture(CapProp.Zoom, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_zoom_" + steptest + ".txt")));
                SetCapture(CapProp.Pan, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_pan_" + steptest + ".txt")));
                SetCapture(CapProp.Tilt, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_tilt_" + steptest + ".txt")));
                SetCapture(CapProp.Contrast, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_contrast_" + steptest + ".txt")));
                SetCapture(CapProp.Brightness, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_brightness_" + steptest + ".txt")));
                SetCapture(CapProp.Focus, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_focus_" + steptest + ".txt")));
                SetCapture(CapProp.Exposure, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_exposure_" + steptest + ".txt")));
                SetCapture(CapProp.Saturation, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_saturation_" + steptest + ".txt")));
                SetCapture(CapProp.Sharpness, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_sharpness_" + steptest + ".txt")));
                SetCapture(CapProp.Gain, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_gain_" + steptest + ".txt")));
                //SetCapture(CapProp.Gamma, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_gamma_" + steptest + ".txt")));
            } catch { }
            try { process_value = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_process_" + steptest + ".txt")); } catch { }
            try { flag_process = Convert.ToBoolean(File.ReadAllText("../../config/test_head_" + head + "_flag_process_" + steptest + ".txt")); } catch { }
            try { rect.X = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_rect_x_" + steptest + ".txt")); } catch { }
            try { rect.Y = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_rect_y_" + steptest + ".txt")); } catch { }
            try { rect.Width = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_rect_width_" + steptest + ".txt")); } catch { }
            try { rect.Height = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_rect_height_" + steptest + ".txt")); } catch { }
            rect_sup = rect;
            try { debug = Convert.ToBoolean(File.ReadAllText("../../config/test_head_" + head + "_debug.txt")); } catch { }
            try { time_out = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_timeout.txt")); } catch { }
            editValueCamDiv3.time = Convert.ToInt32(time_out / 3);
            editValueCamDiv3.state1 = false;
            editValueCamDiv3.state2 = false;
            try { Process_timeout_.Text = File.ReadAllText("../../config/test_head_" + head + "_process_timeout_" + steptest + ".txt"); } catch { }
            try { Process_roi_.Text = File.ReadAllText("../../config/test_head_" + head + "_process_roi_" + steptest + ".txt"); } catch { }
            try { Process_scale_limit_.Text = File.ReadAllText("../../config/test_head_" + head + "_process_scale_limit_" + steptest + ".txt"); } catch { }
            try { Process_scale_next_.Text = File.ReadAllText("../../config/test_head_" + head + "_process_scale_next_" + steptest + ".txt"); } catch { }
            try { ctms_setGammaAddressCsv.Text = File.ReadAllText("../../config/test_head_" + head + "_gamma_" + steptest + ".txt"); } catch { }
            try { flagAutoFocus = Convert.ToBoolean(File.ReadAllText("../../config/camera_show_process_autoFocus" + ".txt")); } catch { }
            try { digitSn = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_digitSn_" + steptest + ".txt")); } catch { }
            ctms_digitSn.Text = digitSn.ToString();
            try { degreeSet = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_degree_" + steptest + ".txt")); } catch { }
            ctms_setDegree.Text = degreeSet.ToString();
            if (flagAutoFocus) {
                ctms_focusAutoTrue.Checked = true;
                ctms_focusAutoFalse.Checked = false;
            } else {
                ctms_focusAutoTrue.Checked = false;
                ctms_focusAutoFalse.Checked = true;
            }
            try {
                string cc = File.ReadAllText("../../config/test_head_" + head + "_bgr_" + steptest + ".txt");
                string[] zz;
                int[] xx = { 0, 0, 0, 0, 0, 0 };
                zz = cc.Split(' ');
                for (int i = 0; i < 6; i++) {
                    xx[i] = Convert.ToInt32(zz[i]);
                }
                bgr_low = new Bgr(xx[0], xx[2], xx[4]);
                bgr_high = new Bgr(xx[1], xx[3], xx[5]);
                cc = File.ReadAllText("../../config/test_head_" + head + "_hsv_" + steptest + ".txt");
                zz = cc.Split(' ');
                for (int i = 0; i < 6; i++) {
                    xx[i] = Convert.ToInt32(zz[i]);
                }
                hsv_low = new Hsv(xx[0], xx[2], xx[4]);
                hsv_high = new Hsv(xx[1], xx[3], xx[5]);
                flag_hsv = Convert.ToBoolean(File.ReadAllText("../../config/test_head_" + head + "_flag_hsv_" + steptest + ".txt"));
                hsv_mask = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_hsv_mask_" + steptest + ".txt"));
                hsv_timeout = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_hsv_timeout_" + steptest + ".txt"));
                stopwatch_hsv_timeout.Restart();
            } catch { }
            try { onebyone.Checked = Convert.ToBoolean(File.ReadAllText("../../config/camera_show_onebyone" + ".txt")); } catch { }
            try { process_roi_set.Checked = Convert.ToBoolean(File.ReadAllText("../../config/camera_show_process_roi_set" + ".txt")); } catch { }
            
            this.Text = head + "." + steptest;
            this.Size = new Size(capture.Width, capture.Height);
            pictureBox1.Size = new Size(capture.Width, capture.Height);
            timeout.Restart();
        }

        private void Form1_Load(object sender, EventArgs e) {
            if (steptest_fail == true) this.Close();
        }

        private void read2d(object sender, EventArgs e) {
            if (flag_return) { flag_return = false; timeout.Restart(); }
            if (timeout.ElapsedMilliseconds >= time_out) { fail_2d(); if (debug == false) return; }
            if (IsMouseDown == true) return;
            if (capture == null || capture.Ptr == IntPtr.Zero || capture.Width == 0) {
                try { capture.Dispose(); } catch { }
                try { OpenCamera(Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_port_read2d.txt"))); } catch { }
                Thread.Sleep(250);
                this.Size = new Size(capture.Width, capture.Height);
                pictureBox1.Size = new Size(capture.Width, capture.Height);
                setup();
                flag_return = true;
                return;
            }
            Mat frame;
            try {
                frame = capture.QueryFrame();
                img = frame.ToImage<Bgr, Byte>();
                Bgr supDegree = new Bgr();
                img = img.Rotate(degreeSet, supDegree);
            } catch (Exception) {
                MessageBox.Show("ไม่สามารถเปิดกล้องได้");
                Application.Exit();
                return;
            }
            Graphics g = Graphics.FromImage(img.Bitmap);
            g.DrawRectangle(Pens.Red, rect);
            if (flag_process == true) {
                Color img_ref;
                Bitmap img_convert;
                img_convert = new Bitmap(img.Bitmap);
                for (int i = 0; i < img_convert.Width; i++) {
                    for (int j = 0; j < img_convert.Height; j++) {
                        img_ref = img_convert.GetPixel(i, j);
                        int gg = (img_ref.R + img_ref.G + img_ref.B) / 3;
                        if (gg < process_value) img_convert.SetPixel(i, j, Color.Black);
                        else img_convert.SetPixel(i, j, Color.White);
                    }
                }
                img.Bitmap = img_convert;

                //Image<Gray, byte> imgOutput;
                //Mat hier = new Mat();
                //imgOutput = img.Convert<Gray, byte>().ThresholdBinary(new Gray(process_value), new Gray(255));
                //Emgu.CV.Util.VectorOfVectorOfPoint contours = new Emgu.CV.Util.VectorOfVectorOfPoint();
            }
            Image<Bgr, byte> img_cut = null;
            img_cut = img.Copy();
            if (rect.Width == 0) { rect.Width = 100; rect.Height = 100; }
            try { img_cut.ROI = rect; } catch { }
            Image<Bgr, byte> temp = img_cut.Copy();

            #region Find triangles and rectangles
            double cannyThresholdLinking = 120.0;
            double cannyThreshold = 180.0;
            Image<Gray, byte> imgOutput = temp.Convert<Gray, byte>().ThresholdBinary(new Gray(150), new Gray(255));
            UMat cannyEdges = new UMat();
            CvInvoke.Canny(imgOutput, cannyEdges, cannyThreshold, cannyThresholdLinking);
            List<RotatedRect> boxList = new List<RotatedRect>(); //a box is a rotated rectangle

            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint()) {
                CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                int count = contours.Size;
                for (int i = 0; i < count; i++) {
                    using (VectorOfPoint contour = contours[i])
                    using (VectorOfPoint approxContour = new VectorOfPoint()) {
                        CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.1, true);
                        if (CvInvoke.ContourArea(approxContour, false) > 50) //only consider contours with area greater than 250
                        {
                            if (approxContour.Size == 4) //The contour has 4 vertices.
                            {
                                #region determine if all the angles in the contour are within [80, 100] degree
                                bool isRectangle = true;
                                Point[] pts = approxContour.ToArray();
                                LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                                for (int j = 0; j < edges.Length; j++) {
                                    double angle = Math.Abs(
                                       edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                                    if (angle < 70 || angle > 100) {
                                        isRectangle = false;
                                        break;
                                    }
                                }
                                #endregion

                                if (isRectangle) { boxList.Add(CvInvoke.MinAreaRect(approxContour)); }
                            }
                        }
                    }
                }
            }
            #endregion

            BarcodeReader reader = new BarcodeReader();
            Result result = reader.Decode(temp.Bitmap);
            Bgr a = new Bgr();
            for (int i = 1; i <= 18; i++) {
                result = reader.Decode(temp.Bitmap);
                if (result != null) {
                    if (result.Text.Length == digitSn) {
                        break;
                    }
                }
                temp = temp.Rotate(5, a);
            }
            if (result == null) {
                foreach (RotatedRect box in boxList) {
                    Rectangle brect = CvInvoke.BoundingRectangle(new VectorOfPointF(box.GetVertices()));
                    Image<Bgr, byte> img_cut_canny = null;
                    img_cut_canny = temp.Copy();
                    img_cut_canny.ROI = brect;
                    Image<Bgr, byte> temp_canny = img_cut_canny.Copy();
                    for (int i = 1; i <= 18; i++) {
                        result = reader.Decode(temp_canny.Bitmap);
                        if (result != null) {
                            if (result.Text.Length == digitSn) {
                                break;
                            }
                        }
                        temp_canny = temp_canny.Rotate(i * 5, a);
                        result = reader.Decode(temp_canny.Bitmap);
                    }
                    if (result != null) {
                        if (result.Text.Length == digitSn) {
                            break;
                        }
                    }
                }
            }

            //BarcodeReader reader = new BarcodeReader();
            //var result = reader.Decode(temp.Bitmap);
            //Bgr a = new Bgr();
            //for (int i = 1; i <= 10; i++) {
            //    result = reader.Decode(temp.Bitmap);
            //    if (result != null) break;
            //    temp = temp.Rotate(i * 8, a);
            //}

            if (result != null) {
                CvInvoke.PutText(img, result.Text, new Point(20, 30), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                if (flag_set_camera == false && debug == false) {
                    File.WriteAllText("test_head_" + head + "_result.txt", result + "\r\nPASS");
                    this.Close();
                }
                flag_result = true;
                result_blackup = result.ToString();

            } else {
                CvInvoke.PutText(img, "not read", new Point(20, 30), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                if (flag_set_camera == false) process_function();
                flag_result = false;
                editValueCamDiv3.Process();
            }
            CvInvoke.PutText(img, boxList.Count.ToString(), new Point(20, 60), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(img, "Port = " + File.ReadAllText("../../config/test_head_" + head + "_port_read2d.txt"), 
                new Point(20, 60), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            pictureBox1.Image = img.Bitmap;
            Thread.Sleep(10);
        }

        private void fail_2d() {
            if (debug == true) { timeout.Reset(); return; }
            File.WriteAllText("test_head_" + head + "_result.txt", "Unreadable\r\nFAIL");
            this.Close();
        }

        private void fail_matching() {
            if (debug == true) { timeout.Reset(); return; }
            File.WriteAllText("test_head_" + head + "_result.txt", "time over\r\nFAIL");
            this.Close();
        }

        private void fail_check_led() {
            if (debug == true) { timeout.Reset(); return; }
            File.WriteAllText("test_head_" + head + "_result.txt", "time over\r\nFAIL");
            this.Close();
        }

        private void compar_image(object sender, EventArgs e) {
            if (flag_return) { flag_return = false; timeout.Restart(); }
            if (timeout.ElapsedMilliseconds >= time_out) { fail_matching(); if (debug == false) return; }
            if (IsMouseDown == true) return;
            if (capture == null || capture.Ptr == IntPtr.Zero || capture.Width == 0) {
                try { capture.Dispose(); } catch { }
                try { OpenCamera(Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_port.txt"))); } catch { }
                Thread.Sleep(250);
                this.Size = new Size(capture.Width, capture.Height);
                pictureBox1.Size = new Size(capture.Width, capture.Height);
                setup();
                flag_return = true;
                return;
            }
            if (capture != null && capture.Ptr != IntPtr.Zero) {
                Mat frame = capture.QueryFrame();
                try {
                    img = frame.ToImage<Bgr, Byte>();
                } catch (Exception) {
                    MessageBox.Show("ไม่สามารถเปิดกล้องได้");
                    Application.Exit();
                    return;
                }
            }
            Graphics g = Graphics.FromImage(img.Bitmap);
            g.DrawRectangle(Pens.Red, rect);
            Image<Bgr, byte> img_cut = null;
            img_cut = img.Copy();
            img_cut.ROI = rect;

            Image<Bgr, Byte> img1 = img_cut.Copy();

            long matchTime;
            try {
                using (Mat modelImage = CvInvoke.Imread("../../config/test_head_" + head + "_" + steptest + ".png", ImreadModes.Grayscale))
                using (Mat observedImage = img1.Mat) {
                    Mat result = DrawMatches.Draw(modelImage, observedImage, out matchTime);
                    CvInvoke.PutText(img, DrawMatches.get_num_object().ToString(), new Point(20, 30), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                    CvInvoke.PutText(img, "Port = " + File.ReadAllText("../../config/test_head_" + head + "_port_read2d.txt"),
                        new Point(20, 60), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                    pictureBox1.Image = img.Bitmap;
                }
                if (DrawMatches.get_num_object() == false) {
                    if (!debug)
                    {
                        editValueCamDiv3.Process();
                    }
                    flag_result = false;
                } else {
                    if (flag_set_camera == false && debug == false) {
                        if (Convert.ToBoolean(File.ReadAllText("../../config/test_head_" + head + "_debug.txt"))) debug = true;
                        else {
                            if (!timeout_show.IsRunning) timeout_show.Restart();
                            if (timeout_show.ElapsedMilliseconds < 50) return;
                        }
                        timeout_show.Stop();
                        int d = 0;
                        string c = "";
                        try {
                            d = Convert.ToInt32(steptest.Substring(steptest.Length - 1, 1));
                            d++;
                            c = steptest.Substring(0, steptest.Length - 1) + d;
                        } catch (Exception) { }
                        if (File.Exists("../../config/test_head_" + head + "_" + steptest + "2.png") ||
                            File.Exists("../../config/test_head_" + head + "_" + c + ".png")) {
                            if (c == "") steptest = steptest + "2";
                            else steptest = c;
                            try {
                                rect.X = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_rect_x_" + steptest + ".txt"));
                                rect.Y = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_rect_y_" + steptest + ".txt"));
                                rect.Width = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_rect_width_" + steptest + ".txt"));
                                rect.Height = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_rect_height_" + steptest + ".txt"));
                            } catch (Exception) { }
                            this.Text = steptest;
                            return;
                        }
                        if (steptest.Contains("image_compar")) {
                            File.WriteAllText("test_head_" + head + "_result.txt", "image detected\r\nNEXT");
                            wait_next();
                            return;
                        } else File.WriteAllText("test_head_" + head + "_result.txt", "image detected\r\nPASS");
                        this.Close();
                    }
                    flag_result = true;
                    result_blackup = "image detected";
                }
            } catch (Exception) {
                CvInvoke.PutText(img, "Port = " + File.ReadAllText("../../config/test_head_" + head + "_port_read2d.txt"),
                        new Point(20, 60), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                pictureBox1.Image = img.Bitmap; 
            }
        }
        private void wait_next() {
            while (true) {
                try {
                    string h = File.ReadAllText("../../config/test_head_" + head + "_compar_image_next_tric.txt");
                } catch { Thread.Sleep(100); continue; }
                File.Delete("../../config/test_head_" + head + "_compar_image_next_tric.txt");
                break;
            }
            try { steptest = File.ReadAllText("../../config/test_head_" + head + "_steptest.txt"); } catch (Exception) { }
            setup();
        }

        private bool flag_return = false;
        private void check_led(object sender, EventArgs e) {
            if (flag_return) { flag_return = false; timeout.Restart(); }
            if (timeout.ElapsedMilliseconds >= time_out) { fail_check_led(); if (debug == false) return; }
            if (IsMouseDown == true) return;
            if (capture == null || capture.Ptr == IntPtr.Zero || capture.Width == 0) {
                try { capture.Dispose(); } catch { }
                try { OpenCamera(Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_port.txt"))); } catch { }
                Thread.Sleep(250);
                this.Size = new Size(capture.Width, capture.Height);
                pictureBox1.Size = new Size(capture.Width, capture.Height);
                setup();
                flag_return = true;
                return;
            }
            Mat frame;
            try {
                if (flag_hsv_test == false) frame = capture.QueryFrame();
                else frame = new Mat("../../config/hsv_test.png");
                img = frame.ToImage<Bgr, Byte>();
                img_hsv = frame.ToImage<Hsv, Byte>();
            } catch (Exception) {
                MessageBox.Show("ไม่สามารถเปิดกล้องได้");
                Application.Exit();
                return;
            }
            Graphics g = Graphics.FromImage(img.Bitmap);
            g.DrawRectangle(Pens.Red, rect);
            Image<Bgr, byte> img_cut = null;
            Image<Hsv, byte> img_cut2 = null;
            Image<Bgr, byte> img1 = null;
            Image<Hsv, byte> img2 = null;
            int redpixels = 0;
            if (flag_hsv == false) {
                img_cut = img.Copy();
                try { img_cut.ROI = rect; } catch { }
                img1 = img_cut.Copy();
                try { redpixels = img1.InRange(bgr_low, bgr_high).CountNonzero()[0]; } catch (Exception) { }
            } else {
                img_cut2 = img_hsv.Copy();
                try { img_cut2.ROI = rect; } catch { }
                img2 = img_cut2.Copy();
                try { redpixels = img2.InRange(hsv_low, hsv_high).CountNonzero()[0]; } catch (Exception) { }
            }
            bool mask = false;
            if (redpixels >= hsv_mask) {
                if (stopwatch_hsv_timeout.ElapsedMilliseconds >= hsv_timeout) mask = true;
            } else {
                stopwatch_hsv_timeout.Restart();
                mask = false;
            }
            CvInvoke.PutText(img, redpixels.ToString(), new Point(20, 30), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(img, mask.ToString(), new Point(20, 60), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(img, stopwatch_hsv_timeout.ElapsedMilliseconds.ToString(), new Point(pictureBox1.Size.Width - 100, 30), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            CvInvoke.PutText(img, "Port = " + File.ReadAllText("../../config/test_head_" + head + "_port_read2d.txt"),
                        new Point(20, 90), FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
            pictureBox1.Image = img.Bitmap;
            if (mask == true) {
                if (flag_set_camera == false && debug == false) {
                    Thread.Sleep(50);
                    File.WriteAllText("test_head_" + head + "_result.txt", "Color detected\r\nPASS");
                    this.Close();
                }
                flag_result = true;
                result_blackup = "Color detected";
            } else {
                flag_result = false;
                editValueCamDiv3.Process();
            }
            //SetCapture(Emgu.CV.CvEnum.CapProp.Focus, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_focus_" + steptest + ".txt")) - 35);
            //SetCapture(Emgu.CV.CvEnum.CapProp.Focus, Convert.ToDouble(File.ReadAllText("../../config/test_head_" + head + "_focus_" + steptest + ".txt")));
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e) {
            if (flag_result == true) {
                if (steptest.Contains("image_compar")) File.WriteAllText("test_head_" + head + "_result.txt", result_blackup + "\r\nNEXT");
                else File.WriteAllText("test_head_" + head + "_result.txt", result_blackup + "\r\nPASS");
            } else {
                File.WriteAllText("test_head_" + head + "_result.txt", "Unreadable\r\nFAIL");
            }
            this.Close();
        }

        private string processAutoStepSup;
        private int contrast_int;
        private int contrast_const;
        private int contrast_int_min;
        private int contrast_int_max;
        private int contrast_config_min = -999;
        private int contrast_config_max = 999;
        private int brightness_int;
        private int brightness_const;
        private int brightness_int_min;
        private int brightness_int_max;
        private int brightness_config_min = -999;
        private int brightness_config_max = 999;
        private int focus_int;
        private int focus_const;
        private int focus_int_min;
        private int focus_int_max;
        private int focus_config_min = -999;
        private int focus_config_max = 999;
        private int sharpness_int;
        private int sharpness_const;
        private int sharpness_int_min;
        private int sharpness_int_max;
        private int sharpness_config_min = -999;
        private int sharpness_config_max = 999;
        private int step_process;
        private int process_scale_limit = 40;
        private int process_scale_next = 2;
        private int process_roi = 10;
        private int process_timeout = 500;
        private bool flag_process_intro = false;
        private static Rectangle rect_sup;
        private int[] rect_int_x = { -1,  0,  1, -1, 1, -1, 0, 1, -2, -1,  0,  1,  2, -2, -1,  0,  1,  2, -2, -1, 1, 2, -2, -1, 0, 1, 2, -2, -1,
            0, 1, 2, -3, -2, -1,  0,  1,  2,  3, -3, -2, -1,  0,  1,  2,  3, -3, -2, -1,  0,  1,  2,  3, -3, -2, -1, 1, 2, 3, -3, -2, -1, 0, 1, 2, 3, -3, -2, -1, 0, 1, 2, 3, -3, -2, -1, 0, 1, 2, 3 };
        private int[] rect_int_y = { -1, -1, -1,  0, 0,  1, 1, 1, -2, -2, -2, -2, -2, -1, -1, -1, -1, -1,  0,  0, 0, 0,  1,  1, 1, 1, 1,  2,  2,
            2, 2, 2, -3, -3, -3, -3, -3, -3, -3, -2, -2, -2, -2, -2, -2, -2, -1, -1, -1, -1, -1, -1, -1,  0,  0,  0, 0, 0, 0,  1,  1,  1, 1, 1, 1, 1,  2,  2,  2, 2, 2, 2, 2,  3,  3,  3, 3, 3, 3, 3 };
        private int rect_array = 0;
        private Stopwatch time_process = new Stopwatch();
        private void process_function() {
            if (flag_process_intro == false) {
                try {
                    contrast_const = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_contrast_" + steptest + ".txt"));
                    brightness_const = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_brightness_" + steptest + ".txt"));
                    focus_const = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_focus_" + steptest + ".txt"));
                    sharpness_const = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_sharpness_" + steptest + ".txt"));
                } catch {
                    contrast_const = (int)capture.GetCaptureProperty(CapProp.Contrast);
                    brightness_const = (int)capture.GetCaptureProperty(CapProp.Brightness);
                    focus_const = (int)capture.GetCaptureProperty(CapProp.Focus);
                    sharpness_const = (int)capture.GetCaptureProperty(CapProp.Sharpness);
                }
                if (steptest_camera_read2d_flag == true) processAutoStepSup = "_read2d";
                try { Process_timeout_.Text = File.ReadAllText("../../config/test_head_" + head + "_process_timeout_" + steptest + ".txt"); } catch { }
                try { Process_roi_.Text = File.ReadAllText("../../config/test_head_" + head + "_process_roi_" + steptest + ".txt"); } catch { }
                try { Process_scale_limit_.Text = File.ReadAllText("../../config/test_head_" + head + "_process_scale_limit_" + steptest + ".txt"); } catch { }
                try { Process_scale_next_.Text = File.ReadAllText("../../config/test_head_" + head + "_process_scale_next_" + steptest + ".txt"); } catch { }

                try { contrast_config_min = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_contrast_min" + processAutoStepSup + ".txt")); } catch { }
                try { contrast_config_max = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_contrast_max" + processAutoStepSup + ".txt")); } catch { }
                try { brightness_config_min = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_brightness_min" + processAutoStepSup + ".txt")); } catch { }
                try { brightness_config_max = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_brightness_max" + processAutoStepSup + ".txt")); } catch { }
                try { focus_config_min = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_focus_min" + processAutoStepSup + ".txt")); } catch { }
                try { focus_config_max = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_focus_max" + processAutoStepSup + ".txt")); } catch { }
                try { sharpness_config_min = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_sharpness_min" + processAutoStepSup + ".txt")); } catch { }
                try { sharpness_config_max = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_sharpness_max" + processAutoStepSup + ".txt")); } catch { }
                process_timeout = Convert.ToInt32(Process_timeout_.Text);
                process_roi = Convert.ToInt32(Process_roi_.Text);
                process_scale_limit = Convert.ToInt32(Process_scale_limit_.Text);
                process_scale_next = Convert.ToInt32(Process_scale_next_.Text);

                contrast_int = contrast_const;
                brightness_int = brightness_const;
                focus_int = focus_const;
                sharpness_int = sharpness_const;

                contrast_int_min = contrast_int - process_scale_limit;
                contrast_int_max = contrast_int + process_scale_limit;
                brightness_int_min = brightness_int - process_scale_limit;
                brightness_int_max = brightness_int + process_scale_limit;
                focus_int_min = focus_int - process_scale_limit;
                focus_int_max = focus_int + process_scale_limit;
                sharpness_int_min = sharpness_int - process_scale_limit;
                sharpness_int_max = sharpness_int + process_scale_limit;

                if (contrast_int_min < contrast_config_min) { contrast_int_min = contrast_config_min; }
                if (contrast_int_max > contrast_config_max) { contrast_int_max = contrast_config_max; }
                if (brightness_int_min < brightness_config_min) { brightness_int_min = brightness_config_min; }
                if (brightness_int_max > brightness_config_max) { brightness_int_max = brightness_config_max; }
                if (focus_int_min < focus_config_min) { focus_int_min = focus_config_min; }
                if (focus_int_max > focus_config_max) { focus_int_max = focus_config_max; }
                if (sharpness_int_min < contrast_config_min) { sharpness_int_min = contrast_config_min; }
                if (sharpness_int_max > contrast_config_max) { sharpness_int_max = contrast_config_max; }

                flag_process_intro = true;
                step_process = 0;
                time_process.Restart();
            }
            if (process_roi == 0) rect_array = rect_int_x.Length;
            switch (step_process) {
                case 0:
                    if (time_process.ElapsedMilliseconds > process_timeout) { time_process.Stop(); step_process++; }
                    break;
                case 1:
                    if (!process_roi_set.Checked) { step_process = 0; break; }
                    if (rect_array == rect_int_x.Length) { rect = rect_sup; step_process = 0; rect_array = 0; break; }
                    rect.X = rect_sup.X + (rect_int_x[rect_array] * process_roi);
                    rect.Y = rect_sup.Y + (rect_int_y[rect_array] * process_roi);
                    rect_array++;
                    break;
                case 2:
                    if (!flagAutoFocus) { step_process++; break; }
                    focus_int -= process_scale_next;
                    if (focus_int <= focus_int_min) { focus_int = focus_const; step_process++; }
                    SetCapture(CapProp.Focus, focus_int);
                    break;
                case 3:
                    if (!flagAutoFocus) { step_process++; break; }
                    focus_int += process_scale_next;
                    if (focus_int >= focus_int_max) { focus_int = focus_const; step_process++; }
                    SetCapture(CapProp.Focus, focus_int);
                    break;
                case 4:
                    contrast_int -= process_scale_next;
                    if (contrast_int <= contrast_int_min) { contrast_int = contrast_const; step_process++; }
                    SetCapture(CapProp.Contrast, contrast_int);
                    break;
                case 5:
                    contrast_int += process_scale_next;
                    if (contrast_int >= contrast_int_max) { contrast_int = contrast_const; step_process++; }
                    SetCapture(CapProp.Contrast, contrast_int);
                    break;
                case 6:
                    brightness_int -= process_scale_next;
                    if (brightness_int <= brightness_int_min) { brightness_int = brightness_const; step_process++; }
                    SetCapture(CapProp.Brightness, brightness_int);
                    break;
                case 7:
                    brightness_int += process_scale_next;
                    if (brightness_int >= brightness_int_max) { brightness_int = brightness_const; step_process++; }
                    SetCapture(CapProp.Brightness, brightness_int);
                    break;
                case 8:
                    sharpness_int -= process_scale_next;
                    if (sharpness_int <= sharpness_int_min) { sharpness_int = sharpness_const; step_process++; }
                    SetCapture(CapProp.Sharpness, sharpness_int);
                    break;
                case 9:
                    sharpness_int += process_scale_next;
                    if (sharpness_int >= sharpness_int_max) { sharpness_int = sharpness_const; step_process = 0; }
                    SetCapture(CapProp.Sharpness, sharpness_int);
                    break;
            }
        }
        /// <summary>
        /// Checlk password of set gamma address
        /// </summary>
        /// <returns>Ture is password success</returns>
        private bool CheckPasswordSetAddress() {
            while (true) {
                KeyPassword form = new KeyPassword();
                DialogResult result = form.ShowDialog();
                if (result != DialogResult.OK) {
                    return false;
                }
                if (form.inputValue != "camera") {
                    MessageBox.Show("Password Error!!");
                    continue;
                }
                break;
            }
            return true;
        }
        /// <summary>
        /// Check address gamma camera in camera and in csv
        /// </summary>
        /// <param name="addressCsv">is gamma address in csv</param>
        /// <returns>True is address success</returns>
        private bool CheckAddressCamera(int addressCsv) {
            double gamma = capture.GetCaptureProperty(CapProp.Gamma);
            if (gamma == addressCsv)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Scan find address camera at same address in csv
        /// </summary>
        /// <param name="addressCsv">is gamma address in csv</param>
        private bool ScanAddressCamera(int addressCsv) {
            for (int loop = 0; loop < 100; loop++) {
                try {
                    capture.Dispose();
                } catch { }
                //if (loop == Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_port_read2d.txt"))) {
                //    continue;
                //}
                OpenCamera(loop);
                if (capture.Width == 0)
                {
                    if (loop < 8)
                    {
                        //MessageBox.Show(loop + ". Width=0");
                        continue;
                    }
                    if (debug)
                    {
                        MessageBox.Show("Not find camera address " + address + "[" + addressCsv + "]" + " loop=" + loop);
                    }
                    OpenCamera(0);
                    return false;
                }
                if (!CheckAddressCamera(addressCsv)) {
                    //MessageBox.Show(loop + ". Address Fail");
                    continue;
                }
                SetPortCameraFollowAddress(loop);
                break;
            }
            return true;
        }

        /// <summary>
        /// Set new port camera follow address in csv
        /// </summary>
        /// <param name="portNew">is New port</param>
        private void SetPortCameraFollowAddress(int portNew) {
            if (steptest_camera_read2d_flag == true)
            {
                File.WriteAllText("../../config/test_head_" + head + "_port_read2d.txt", portNew.ToString());
            }
            else
            {
                File.WriteAllText("../../config/test_head_" + head + "_port.txt", portNew.ToString());
            }
        }
        /// <summary>
        /// Read file set Address
        /// </summary>
        private void ReadFileAddress() {
            address = Address.gamma;
            if (steptest_camera_read2d_flag)
            {
                try
                {
                    address = File.ReadAllText("../../config/test_head_" + head + "_address_read2d.txt");
                } catch { }
            }
            else
            {
                try
                {
                    address = File.ReadAllText("../../config/test_head_" + head + "_address.txt");
                } catch { }
            }
            if (address == Address.gamma)
            {
                ctms_setGammaAddress.Checked = true;
            }
            else if(address == Address.port)
            {
                ctms_usePort.Checked = true;
            }
        }
        /// <summary>
        /// For read value config camera current to csv
        /// </summary>
        private void ReadConfigCameraToCsvFile() {
            double zoom = capture.GetCaptureProperty(CapProp.Zoom);
            double pan = capture.GetCaptureProperty(CapProp.Pan);
            double tilt = capture.GetCaptureProperty(CapProp.Tilt);
            double contrast = capture.GetCaptureProperty(CapProp.Contrast);
            double brightness = capture.GetCaptureProperty(CapProp.Brightness);
            double focus = capture.GetCaptureProperty(CapProp.Focus);
            double exposure = capture.GetCaptureProperty(CapProp.Exposure);
            double saturation = capture.GetCaptureProperty(CapProp.Saturation);
            double sharpness = capture.GetCaptureProperty(CapProp.Sharpness);
            double gain = capture.GetCaptureProperty(CapProp.Gain);
            double gamma = capture.GetCaptureProperty(CapProp.Gamma);

            File.WriteAllText("../../config/test_head_" + head + "_zoom_" + steptest + ".txt", zoom.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_pan_" + steptest + ".txt", pan.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_tilt_" + steptest + ".txt", tilt.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_contrast_" + steptest + ".txt", contrast.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_brightness_" + steptest + ".txt", brightness.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_focus_" + steptest + ".txt", focus.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_exposure_" + steptest + ".txt", exposure.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_saturation_" + steptest + ".txt", saturation.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_sharpness_" + steptest + ".txt", sharpness.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_gain_" + steptest + ".txt", gain.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_gamma_" + steptest + ".txt", gamma.ToString());
        }

        /// <summary>
        /// Open camera
        /// </summary>
        /// <param name="port"></param>
        private void OpenCamera(int port) {
            try
            {
                capture.Dispose();
            } catch { }
            capture = new VideoCapture(port, captureApi);
        }

        /// <summary>
        /// Log program catch to csv file
        /// </summary>
        /// <param name="text"></param>
        private void LogProgramCatch(string text) {
            string path = "D:\\LogError\\CameraCatch";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            DateTime now = DateTime.Now;
            StreamWriter swOut = new StreamWriter(path + "\\" + now.Year + "_" + now.Month + ".csv", true);
            string time = now.Day.ToString("00") + ":" + now.Hour.ToString("00") + ":" + now.Minute.ToString("00") + ":" + now.Second.ToString("00");
            swOut.WriteLine(time + "," + text);
            swOut.Close();
        }

        /// <summary>
        /// Event Exception Catch Program
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            LogProgramCatch(e.ExceptionObject.ToString());
        }

        Form f1;
        HScrollBar h_zoom;
        Label l_zoom;
        Label s_zoom;
        HScrollBar h_pan;
        Label l_pan;
        Label s_pan;
        HScrollBar h_tilt;
        Label l_tilt;
        Label s_tilt;
        HScrollBar h_contrast;
        Label l_contrast;
        Label s_contrast;
        HScrollBar h_brightness;
        Label l_brightness;
        Label s_brightness;
        HScrollBar h_focus;
        Label l_focus;
        Label s_focus;
        HScrollBar h_process;
        Label l_process;
        Label s_process;
        Button b_process;
        Label l_bgr;
        TextBox t_bgr;
        Label l_hsv;
        TextBox t_hsv;
        Button b_hsv;
        TextBox t_hsv_mask;
        Label l_mask;
        Label l_hsv_test;
        Label l_timeout;
        TextBox t_timeout;
        Button b_example;
        CheckBox show_all;
        HScrollBar h_exposure;
        Label l_exposure;
        Label s_exposure;
        HScrollBar h_saturation;
        Label l_saturation;
        Label s_saturation;
        HScrollBar h_sharpness;
        Label l_sharpness;
        Label s_sharpness;
        HScrollBar h_gain;
        Label l_gain;
        Label s_gain;
        HScrollBar h_gamma;
        Label l_gamma;
        Label s_gamma;
        private void setCameraToolStripMenuItem_Click(object sender, EventArgs e) {
            //if (flag_add_step) return;
            flag_set_camera = true;
            f1 = new Form();
            f1.FormClosed += F1_FormClosed;
            f1.Size = new Size(400, 400);
            h_zoom = new HScrollBar();
            l_zoom = new Label();
            s_zoom = new Label();
            h_pan = new HScrollBar();
            l_pan = new Label();
            s_pan = new Label();
            h_tilt = new HScrollBar();
            l_tilt = new Label();
            s_tilt = new Label();
            h_contrast = new HScrollBar();
            l_contrast = new Label();
            s_contrast = new Label();
            h_brightness = new HScrollBar();
            l_brightness = new Label();
            s_brightness = new Label();
            h_focus = new HScrollBar();
            l_focus = new Label();
            s_focus = new Label();
            h_process = new HScrollBar();
            l_process = new Label();
            s_process = new Label();
            b_process = new Button();
            l_bgr = new Label();
            t_bgr = new TextBox();
            l_hsv = new Label();
            t_hsv = new TextBox();
            b_hsv = new Button();
            t_hsv_mask = new TextBox();
            l_mask = new Label();
            l_hsv_test = new Label();
            l_timeout = new Label();
            t_timeout = new TextBox();
            b_example = new Button();
            show_all = new CheckBox();
            h_exposure = new HScrollBar();
            l_exposure = new Label();
            s_exposure = new Label();
            h_saturation = new HScrollBar();
            l_saturation = new Label();
            s_saturation = new Label();
            h_sharpness = new HScrollBar();
            l_sharpness = new Label();
            s_sharpness = new Label();
            h_gain = new HScrollBar();
            l_gain = new Label();
            s_gain = new Label();
            h_gamma = new HScrollBar();
            l_gamma = new Label();
            s_gamma = new Label();
            string str_read2d = "";
            bool flag_try_min_max_value = false;
            if (steptest_camera_read2d_flag == true) str_read2d = "_read2d";

            l_zoom.Text = "zoom";
            l_zoom.Size = new Size(50, 15);
            l_zoom.Location = new Point(1, 1);
            f1.Controls.Add(l_zoom);
            h_zoom.Scroll += H_zoom_Scroll;
            h_zoom.LargeChange = 1;
            //try { h_zoom.Minimum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_zoom_min" + str_read2d + ".txt")); } catch (Exception) { h_zoom.Minimum = -999; }
            //try { h_zoom.Maximum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_zoom_max" + str_read2d + ".txt")); } catch (Exception) { h_zoom.Maximum = 999; }
            //try { h_zoom.Value = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_zoom_" + steptest + ".txt")); } catch { flag_try_min_max_value = true; }
            //if (flag_try_min_max_value) {
            //    try { h_zoom.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Zoom); } catch { }
            //    flag_try_min_max_value = false;
            //}
            h_zoom.Minimum = -999;
            h_zoom.Maximum = 999;
            try
            {
                h_zoom.Value = (int)capture.GetCaptureProperty(CapProp.Saturation);
            } catch { }
            h_zoom.Enabled = false;
            h_zoom.Size = new Size(300, h_zoom.Height);
            h_zoom.Location = new Point(1, 15);
            f1.Controls.Add(h_zoom);
            s_zoom.Text = h_zoom.Value.ToString();
            s_zoom.Size = new Size(50, 15);
            s_zoom.Location = new Point(h_zoom.Size.Width + 5, h_zoom.Location.Y + 2);
            f1.Controls.Add(s_zoom);

            l_pan.Text = "pan";
            l_pan.Size = new Size(300, 15);
            l_pan.Location = new Point(1, 40);
            f1.Controls.Add(l_pan);
            h_pan.Scroll += H_pan_Scroll;
            h_pan.LargeChange = 1;
            //try { h_pan.Minimum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_pan_min" + str_read2d + ".txt")); } catch (Exception) { h_pan.Minimum = -999; }
            //try { h_pan.Maximum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_pan_max" + str_read2d + ".txt")); } catch (Exception) { h_pan.Maximum = 999; }
            //try { h_pan.Value = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_pan_" + steptest + ".txt")); } catch { flag_try_min_max_value = true; }
            //if (flag_try_min_max_value) {
            //    try { h_pan.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Pan); } catch { }
            //    flag_try_min_max_value = false;
            //}
            h_pan.Minimum = -999;
            h_pan.Maximum = 999;
            try
            {
                h_pan.Value = (int)capture.GetCaptureProperty(CapProp.Pan);
            } catch { }
            h_pan.Enabled = false;
            h_pan.Size = new Size(300, h_pan.Height);
            h_pan.Location = new Point(1, 55);
            f1.Controls.Add(h_pan);
            s_pan.Text = h_pan.Value.ToString();
            s_pan.Size = new Size(300, 15);
            s_pan.Location = new Point(h_pan.Size.Width + 5, h_pan.Location.Y + 2);
            f1.Controls.Add(s_pan);

            l_tilt.Text = "tilt";
            l_tilt.Size = new Size(300, 15);
            l_tilt.Location = new Point(1, 80);
            f1.Controls.Add(l_tilt);
            h_tilt.Scroll += H_tilt_Scroll;
            h_tilt.LargeChange = 1;
            //try { h_tilt.Minimum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_tilt_min" + str_read2d + ".txt")); } catch (Exception) { h_tilt.Minimum = -999; }
            //try { h_tilt.Maximum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_tilt_max" + str_read2d + ".txt")); } catch (Exception) { h_tilt.Maximum = 999; }
            //try { h_tilt.Value = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_tilt_" + steptest + ".txt")); } catch { flag_try_min_max_value = true; }
            //if (flag_try_min_max_value) {
            //    try { h_tilt.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Tilt); } catch { }
            //    flag_try_min_max_value = false;
            //}
            h_tilt.Minimum = -999;
            h_tilt.Maximum = 999;
            try
            {
                h_tilt.Value = (int)capture.GetCaptureProperty(CapProp.Tilt);
            } catch { }
            h_tilt.Enabled = false;
            h_tilt.Size = new Size(300, h_tilt.Height);
            h_tilt.Location = new Point(1, 95);
            f1.Controls.Add(h_tilt);
            s_tilt.Text = h_tilt.Value.ToString();
            s_tilt.Size = new Size(300, 15);
            s_tilt.Location = new Point(h_tilt.Size.Width + 5, h_tilt.Location.Y + 2);
            f1.Controls.Add(s_tilt);

            l_contrast.Text = "contrast";
            l_contrast.Size = new Size(300, 15);
            l_contrast.Location = new Point(1, 120);
            f1.Controls.Add(l_contrast);
            h_contrast.Scroll += H_contrast_Scroll;
            h_contrast.LargeChange = 1;
            //try { h_contrast.Minimum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_contrast_min" + str_read2d + ".txt")); } catch (Exception) { h_contrast.Minimum = -999; }
            //try { h_contrast.Maximum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_contrast_max" + str_read2d + ".txt")); } catch (Exception) { h_contrast.Maximum = 999; }
            //try { h_contrast.Value = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_contrast_" + steptest + ".txt")); } catch { flag_try_min_max_value = true; }
            //if (flag_try_min_max_value) {
            //    try { h_contrast.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Contrast); } catch { }
            //    flag_try_min_max_value = false;
            //}
            h_contrast.Minimum = -999;
            h_contrast.Maximum = 999;
            try
            {
                h_contrast.Value = (int)capture.GetCaptureProperty(CapProp.Contrast);
            } catch { }
            h_contrast.Enabled = false;
            h_contrast.Size = new Size(300, h_contrast.Height);
            h_contrast.Location = new Point(1, 135);
            f1.Controls.Add(h_contrast);
            s_contrast.Text = h_contrast.Value.ToString();
            s_contrast.Size = new Size(300, 15);
            s_contrast.Location = new Point(h_contrast.Size.Width + 5, h_contrast.Location.Y + 2);
            f1.Controls.Add(s_contrast);

            l_brightness.Text = "brightness";
            l_brightness.Size = new Size(300, 15);
            l_brightness.Location = new Point(1, 160);
            f1.Controls.Add(l_brightness);
            h_brightness.Scroll += H_brightness_Scroll;
            h_brightness.LargeChange = 1;
            //try { h_brightness.Minimum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_brightness_min" + str_read2d + ".txt")); } catch (Exception) { h_brightness.Minimum = -999; }
            //try { h_brightness.Maximum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_brightness_max" + str_read2d + ".txt")); } catch (Exception) { h_brightness.Maximum = 999; }
            //try { h_brightness.Value = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_brightness_" + steptest + ".txt")); } catch { flag_try_min_max_value = true; }
            //if (flag_try_min_max_value) {
            //    try { h_brightness.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Brightness); } catch { }
            //    flag_try_min_max_value = false;
            //}
            h_brightness.Minimum = -999;
            h_brightness.Maximum = 999;
            try
            {
                h_brightness.Value = (int)capture.GetCaptureProperty(CapProp.Brightness);
            } catch { }
            h_brightness.Enabled = false;
            h_brightness.Size = new Size(300, h_brightness.Height);
            h_brightness.Location = new Point(1, 175);
            f1.Controls.Add(h_brightness);
            s_brightness.Text = h_brightness.Value.ToString();
            s_brightness.Size = new Size(300, 15);
            s_brightness.Location = new Point(h_brightness.Size.Width + 5, h_brightness.Location.Y + 2);
            f1.Controls.Add(s_brightness);

            l_focus.Text = "focus";
            l_focus.Size = new Size(300, 15);
            l_focus.Location = new Point(1, 200);
            f1.Controls.Add(l_focus);
            h_focus.Scroll += H_focus_Scroll;
            h_focus.LargeChange = 1;
            //try { h_focus.Minimum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_focus_min" + str_read2d + ".txt")); } catch (Exception) { h_focus.Minimum = -999; }
            //try { h_focus.Maximum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_focus_max" + str_read2d + ".txt")); } catch (Exception) { h_focus.Maximum = 999; }
            //try { h_focus.Value = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_focus_" + steptest + ".txt")); } catch { flag_try_min_max_value = true; }
            //if (flag_try_min_max_value) {
            //    try { h_focus.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Focus); } catch { }
            //    flag_try_min_max_value = false;
            //}
            h_focus.Minimum = -999;
            h_focus.Maximum = 999;
            try
            {
                h_focus.Value = (int)capture.GetCaptureProperty(CapProp.Focus);
            } catch { }
            h_focus.Enabled = false;
            h_focus.Size = new Size(300, h_focus.Height);
            h_focus.Location = new Point(1, 215);
            f1.Controls.Add(h_focus);
            s_focus.Text = h_focus.Value.ToString();
            s_focus.Size = new Size(30, 15);
            s_focus.Location = new Point(h_focus.Size.Width + 5, h_focus.Location.Y + 2);
            f1.Controls.Add(s_focus);
            Button b_focus = new Button();
            b_focus.Click += B_focus_Click;
            b_focus.Text = "auto";
            b_focus.Visible = false;
            b_focus.Size = new Size(40, 20);
            b_focus.Location = new Point(h_focus.Size.Width + 40, h_focus.Location.Y);
            f1.Controls.Add(b_focus);

            l_process.Text = "process";
            l_process.Size = new Size(300, 15);
            l_process.Location = new Point(1, 240);
            f1.Controls.Add(l_process);
            h_process.Scroll += H_process_Scroll;
            h_process.LargeChange = 1;
            h_process.Minimum = 0;
            h_process.Maximum = 255;
            h_process.Value = process_value;
            h_process.Size = new Size(300, h_process.Height);
            h_process.Location = new Point(1, 255);
            f1.Controls.Add(h_process);
            s_process.Text = h_process.Value.ToString();
            s_process.Size = new Size(30, 15);
            s_process.Location = new Point(h_process.Size.Width + 5, h_process.Location.Y + 2);
            f1.Controls.Add(s_process);
            b_process.Click += B_process_Click;
            b_process.Text = flag_process.ToString();
            b_process.Size = new Size(40, 20);
            b_process.Visible = false;
            b_process.Location = new Point(h_process.Size.Width + 40, h_process.Location.Y);
            f1.Controls.Add(b_process);

            l_bgr.Text = "bgr: \"BlurLow BlueHigh GreenLow GreenHigh RedLow RedHigh\"";
            l_bgr.Size = new Size(400, 15);
            l_bgr.Location = new Point(1, 280);
            f1.Controls.Add(l_bgr);
            t_bgr.Text = bgr_low.Blue.ToString() + " " + bgr_high.Blue.ToString() + " " +
                         bgr_low.Green.ToString() + " " + bgr_high.Green.ToString() + " " +
                         bgr_low.Red.ToString() + " " + bgr_high.Red.ToString();
            t_bgr.Size = new Size(180, 20);
            t_bgr.Location = new Point(1, l_bgr.Location.Y + 15);
            t_bgr.KeyDown += T_bgr_KeyDown;
            f1.Controls.Add(t_bgr);
            l_mask.Text = "mask :";
            l_mask.Size = new Size(40, 15);
            l_mask.Location = new Point(t_bgr.Size.Width + 85, t_bgr.Location.Y + 2);
            f1.Controls.Add(l_mask);
            t_hsv_mask.Text = hsv_mask.ToString();
            t_hsv_mask.Size = new Size(75, 20);
            t_hsv_mask.Location = new Point(t_bgr.Size.Width + 125, t_bgr.Location.Y);
            t_hsv_mask.KeyDown += T_hsv_mask_KeyDown;
            f1.Controls.Add(t_hsv_mask);
            b_example.Click += B_example_Click;
            b_example.Text = "example";
            b_example.Size = new Size(60, 20);
            b_example.Location = new Point(t_bgr.Size.Width + 10, t_bgr.Location.Y);
            f1.Controls.Add(b_example);

            l_hsv.Text = "hsv: \"HueLow HueHigh SatuationLow SatuationHigh ValueLow ValueHigh\"";
            l_hsv.Size = new Size(400, 15);
            l_hsv.Location = new Point(1, 320);
            f1.Controls.Add(l_hsv);
            t_hsv.Text = hsv_low.Hue.ToString() + " " + hsv_high.Hue.ToString() + " " +
                         hsv_low.Satuation.ToString() + " " + hsv_high.Satuation.ToString() + " " +
                         hsv_low.Value.ToString() + " " + hsv_high.Value.ToString();
            t_hsv.Size = new Size(180, 20);
            t_hsv.Location = new Point(1, l_hsv.Location.Y + 15);
            t_hsv.KeyDown += T_hsv_KeyDown;
            f1.Controls.Add(t_hsv);
            l_timeout.Text = "timeout :";
            l_timeout.Size = new Size(47, 15);
            l_timeout.Location = new Point(t_hsv.Size.Width + 10, t_hsv.Location.Y + 2);
            f1.Controls.Add(l_timeout);
            t_timeout.Text = hsv_timeout.ToString();
            t_timeout.Size = new Size(60, 20);
            t_timeout.Location = new Point(t_hsv.Size.Width + 57, t_hsv.Location.Y);
            t_timeout.KeyDown += T_timeout_KeyDown;
            f1.Controls.Add(t_timeout);
            l_hsv_test.Text = "ms";
            l_hsv_test.Size = new Size(30, 15);
            l_hsv_test.Location = new Point(t_hsv.Size.Width + 120, t_hsv.Location.Y + 2);
            f1.Controls.Add(l_hsv_test);
            b_hsv.Click += B_hsv_Click;
            b_hsv.Text = flag_hsv_test.ToString();
            b_hsv.Size = new Size(40, 20);
            b_hsv.Location = new Point(t_hsv.Size.Width + 160, t_hsv.Location.Y);
            f1.Controls.Add(b_hsv);

            show_all.Location = new Point(365, 0);
            show_all.Click += Show_all_Click;
            f1.Controls.Add(show_all);

            l_exposure.Text = "exposure";
            l_exposure.Size = new Size(300, 15);
            l_exposure.Location = new Point(1, 360);
            f1.Controls.Add(l_exposure);
            h_exposure.Scroll += H_exposure_Scroll;
            h_exposure.LargeChange = 1;
            //try { h_exposure.Minimum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_exposure_min" + str_read2d + ".txt")); } catch (Exception) { h_exposure.Minimum = -999; }
            //try { h_exposure.Maximum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_exposure_max" + str_read2d + ".txt")); } catch (Exception) { h_exposure.Maximum = 999; }
            //try { h_exposure.Value = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_exposure_" + steptest + ".txt")); } catch { flag_try_min_max_value = true; }
            //if (flag_try_min_max_value) {
            //    try { h_exposure.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Exposure); } catch { }
            //    flag_try_min_max_value = false;
            //}
            h_exposure.Minimum = -999;
            h_exposure.Maximum = 999;
            try
            {
                h_exposure.Value = (int)capture.GetCaptureProperty(CapProp.Exposure);
            } catch { }
            h_exposure.Enabled = false;
            h_exposure.Size = new Size(300, h_exposure.Height);
            h_exposure.Location = new Point(1, 375);
            f1.Controls.Add(h_exposure);
            s_exposure.Text = h_exposure.Value.ToString();
            s_exposure.Size = new Size(300, 15);
            s_exposure.Location = new Point(h_exposure.Size.Width + 5, h_exposure.Location.Y + 2);
            f1.Controls.Add(s_exposure);

            l_saturation.Text = "saturation" + " (Address In Camera)";
            l_saturation.Size = new Size(300, 15);
            l_saturation.Location = new Point(1, 400);
            f1.Controls.Add(l_saturation);
            h_saturation.Scroll += H_saturation_Scroll;
            h_saturation.LargeChange = 1;
            //try { h_saturation.Minimum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_saturation_min" + str_read2d + ".txt")); } catch (Exception) { h_saturation.Minimum = -999; }
            //try { h_saturation.Maximum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_saturation_max" + str_read2d + ".txt")); } catch (Exception) { h_saturation.Maximum = 999; }
            //try { h_saturation.Value = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_saturation_" + steptest + ".txt")); } catch { flag_try_min_max_value = true; }
            h_saturation.Minimum = -999;
            h_saturation.Maximum = 999;
            try
            {
                h_saturation.Value = (int)capture.GetCaptureProperty(CapProp.Saturation);
            } catch { }
            h_saturation.Size = new Size(300, h_saturation.Height);
            h_saturation.Location = new Point(1, 415);
            h_saturation.Enabled = false;
            f1.Controls.Add(h_saturation);
            s_saturation.Text = h_saturation.Value.ToString();
            s_saturation.Size = new Size(300, 15);
            s_saturation.Location = new Point(h_saturation.Size.Width + 5, h_saturation.Location.Y + 2);
            f1.Controls.Add(s_saturation);

            l_sharpness.Text = "sharpness";
            l_sharpness.Size = new Size(300, 15);
            l_sharpness.Location = new Point(1, 440);
            f1.Controls.Add(l_sharpness);
            h_sharpness.Scroll += H_sharpness_Scroll;
            h_sharpness.LargeChange = 1;
            //try { h_sharpness.Minimum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_sharpness_min" + str_read2d + ".txt")); } catch (Exception) { h_sharpness.Minimum = -999; }
            //try { h_sharpness.Maximum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_sharpness_max" + str_read2d + ".txt")); } catch (Exception) { h_sharpness.Maximum = 999; }
            //try { h_sharpness.Value = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_sharpness_" + steptest + ".txt")); } catch { flag_try_min_max_value = true; }
            //if (flag_try_min_max_value) {
            //    try { h_sharpness.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Sharpness); } catch { }
            //    flag_try_min_max_value = false;
            //}
            h_sharpness.Minimum = -999;
            h_sharpness.Maximum = 999;
            try
            {
                h_sharpness.Value = (int)capture.GetCaptureProperty(CapProp.Sharpness);
            } catch { }
            h_sharpness.Enabled = false;
            h_sharpness.Size = new Size(300, h_sharpness.Height);
            h_sharpness.Location = new Point(1, 455);
            f1.Controls.Add(h_sharpness);
            s_sharpness.Text = h_sharpness.Value.ToString();
            s_sharpness.Size = new Size(300, 15);
            s_sharpness.Location = new Point(h_sharpness.Size.Width + 5, h_sharpness.Location.Y + 2);
            f1.Controls.Add(s_sharpness);

            l_gain.Text = "gain" + " (Address In Camera)";
            l_gain.Size = new Size(300, 15);
            l_gain.Location = new Point(1, 480);
            f1.Controls.Add(l_gain);
            h_gain.Scroll += H_gain_Scroll;
            h_gain.LargeChange = 1;
            //try { h_gain.Minimum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_gain_min" + str_read2d + ".txt")); } catch (Exception) { h_gain.Minimum = -999; }
            //try { h_gain.Maximum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_gain_max" + str_read2d + ".txt")); } catch (Exception) { h_gain.Maximum = 999; }
            //try { h_gain.Value = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_gain_" + steptest + ".txt")); } catch { flag_try_min_max_value = true; }
            h_gain.Minimum = -999;
            h_gain.Maximum = 999;
            try
            {
                h_gain.Value = (int)capture.GetCaptureProperty(CapProp.Gain); 
            } catch { }
            h_gain.Size = new Size(300, h_gain.Height);
            h_gain.Location = new Point(1, 495);
            h_gain.Enabled = false;
            f1.Controls.Add(h_gain);
            s_gain.Text = h_gain.Value.ToString();
            s_gain.Size = new Size(300, 15);
            s_gain.Location = new Point(h_gain.Size.Width + 5, h_gain.Location.Y + 2);
            f1.Controls.Add(s_gain);

            l_gamma.Text = "gamma (Address in camera)";
            l_gamma.Size = new Size(300, 15);
            l_gamma.Location = new Point(1, 520);
            f1.Controls.Add(l_gamma);
            h_gamma.Scroll += H_gamma_Scroll;
            h_gamma.LargeChange = 1;
            //try { h_gamma.Minimum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_gamma_min" + str_read2d + ".txt")); } catch (Exception) { h_gamma.Minimum = -999; }
            //try { h_gamma.Maximum = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_cam_gamma_max" + str_read2d + ".txt")); } catch (Exception) { h_gamma.Maximum = 999; }
            //try { h_gamma.Value = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + head + "_gamma_" + steptest + ".txt")); } catch { flag_try_min_max_value = true; }
            h_gamma.Minimum = -999;
            h_gamma.Maximum = 999;
            try { h_gamma.Value = (int)capture.GetCaptureProperty(CapProp.Gamma); } catch { }
            h_gamma.Size = new Size(300, h_gamma.Height);
            h_gamma.Location = new Point(1, 535);
            h_gamma.Enabled = false;
            f1.Controls.Add(h_gamma);
            s_gamma.Text = h_gamma.Value.ToString();
            s_gamma.Size = new Size(300, 15);
            s_gamma.Location = new Point(h_gamma.Size.Width + 5, h_gamma.Location.Y + 2);
            f1.Controls.Add(s_gamma);

            f1.Show();
        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e) {
            string name_header = this.Text;
            this.Text = "...";
            string str_read2d = "";
            int value = 0;
            bool next = false;
            double min = 0, max = 0;
            if (steptest_camera_read2d_flag == true) str_read2d = "_read2d";
            var emgu_cv_cvenum = new[] { Emgu.CV.CvEnum.CapProp.Zoom,
                                         Emgu.CV.CvEnum.CapProp.Exposure,
                                         Emgu.CV.CvEnum.CapProp.Pan,
                                         Emgu.CV.CvEnum.CapProp.Tilt,
                                         Emgu.CV.CvEnum.CapProp.Contrast,
                                         Emgu.CV.CvEnum.CapProp.Brightness,
                                         Emgu.CV.CvEnum.CapProp.Focus,
                                         Emgu.CV.CvEnum.CapProp.Saturation,
                                         Emgu.CV.CvEnum.CapProp.Sharpness};
            string[] str_step = { "zoom",
                                  "exposure",
                                  "pan",
                                  "tilt",
                                  "contrast",
                                  "brightness",
                                  "focus",
                                  "sharpness"};
            int[] length = { 100,
                             100,
                             100,
                             100,
                             200,
                             200,
                             300,
                             200};
            int[] increment = { 1,
                                1,
                                1,
                                1,
                                1,
                                1,
                                5,
                                1};
            for (int i = 0; i < emgu_cv_cvenum.Length; i++) {
                min = 0;
                max = 0;
                value = 0;
                try { value = (int)capture.GetCaptureProperty(emgu_cv_cvenum[i]); } catch (Exception) { }
                next = false;
                int increment_ = 1;
                for (double gh = value - length[i]; gh < value + length[i]; gh += 1) {
                    SetCapture(emgu_cv_cvenum[i], gh);
                    if (str_step[i] == "focus") SetCapture(emgu_cv_cvenum[i], gh);
                    if (gh != capture.GetCaptureProperty(emgu_cv_cvenum[i])) {
                        if (!next) continue;
                        if (increment_ <= increment[i]) { increment_++; continue; };
                        break;
                    }
                    if (!next) { min = gh; max = gh; next = true; }
                    max = gh;
                    increment_ = 1;
                }
                if (onebyone.Checked) {
                    File.WriteAllText("../../config/test_head_" + head + "_cam_" + str_step[i] + "_max.txt", max.ToString());
                    File.WriteAllText("../../config/test_head_" + head + "_cam_" + str_step[i] + "_min.txt", min.ToString());
                    File.WriteAllText("../../config/test_head_" + head + "_cam_" + str_step[i] + "_max_read2d.txt", max.ToString());
                    File.WriteAllText("../../config/test_head_" + head + "_cam_" + str_step[i] + "_min_read2d.txt", min.ToString());
                } else {
                    File.WriteAllText("../../config/test_head_" + head + "_cam_" + str_step[i] + "_max" + str_read2d + ".txt", max.ToString());
                    File.WriteAllText("../../config/test_head_" + head + "_cam_" + str_step[i] + "_min" + str_read2d + ".txt", min.ToString());
                }
                ToolStripMenuItem m = new ToolStripMenuItem();
                for (int h = 1; h <= 10; h++) {
                    switch (h) {
                        case 1: m = config_cam1; break;
                        case 2: m = config_cam2; break;
                        case 3: m = config_cam3; break;
                        case 4: m = config_cam4; break;
                        case 5: m = config_cam5; break;
                        case 6: m = config_cam6; break;
                        case 7: m = config_cam7; break;
                        case 8: m = config_cam8; break;
                        case 9: m = config_cam9; break;
                        case 10: m = config_cam10; break;
                    }
                    if (m.Checked) {
                        if (onebyone.Checked) {
                            File.WriteAllText("../../config/test_head_" + h + "_cam_" + str_step[i] + "_max.txt", max.ToString());
                            File.WriteAllText("../../config/test_head_" + h + "_cam_" + str_step[i] + "_min.txt", min.ToString());
                            File.WriteAllText("../../config/test_head_" + h + "_cam_" + str_step[i] + "_max_read2d.txt", max.ToString());
                            File.WriteAllText("../../config/test_head_" + h + "_cam_" + str_step[i] + "_min_read2d.txt", min.ToString());
                        } else {
                            File.WriteAllText("../../config/test_head_" + h + "_cam_" + str_step[i] + "_max" + str_read2d + ".txt", max.ToString());
                            File.WriteAllText("../../config/test_head_" + h + "_cam_" + str_step[i] + "_min" + str_read2d + ".txt", min.ToString());
                        }
                    }
                }
                SetCapture(emgu_cv_cvenum[i], value);
            }
            this.Text = name_header;
        }

        private void B_example_Click(object sender, EventArgs e) {
            MessageBox.Show("green : bgr : 0 100 100 255 0 50" +
                            "red : hsv : 0 60 0 255 150 255");
        }

        private void T_timeout_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyValue != 13) return;
            string cc = t_timeout.Text;
            int aa;
            try {
                aa = Convert.ToInt32(cc);
            } catch (Exception) { MessageBox.Show("not formath"); return; }
            hsv_timeout = aa;
        }

        private void T_hsv_mask_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyValue != 13) return;
            string cc = t_hsv_mask.Text;
            int aa;
            try {
                aa = Convert.ToInt32(cc);
            } catch (Exception) { MessageBox.Show("not formath"); return; }
            hsv_mask = aa;
        }

        private void B_hsv_Click(object sender, EventArgs e) {
            if (b_hsv.Text == "True") {
                b_hsv.Text = "False";
                flag_hsv_test = false;
            } else {
                b_hsv.Text = "True";
                flag_hsv_test = true;
            }
        }

        private void T_hsv_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyValue != 13) return;
            string cc = t_hsv.Text;
            string[] zz;
            int[] xx = { 0, 0, 0, 0, 0, 0 };
            try {
                zz = cc.Split(' ');
            } catch (Exception) { MessageBox.Show("not formath"); return; }
            if (zz.Length != 6) { MessageBox.Show("not formath"); return; }
            try {
                for (int i = 0; i < 6; i++) {
                    xx[i] = Convert.ToInt32(zz[i]);
                }
            } catch (Exception) { MessageBox.Show("not formath"); return; }
            hsv_low = new Hsv(xx[0], xx[2], xx[4]);
            hsv_high = new Hsv(xx[1], xx[3], xx[5]);
            flag_hsv = true;
        }

        private void T_bgr_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyValue != 13) return;
            string cc = t_bgr.Text;
            string[] zz;
            int[] xx = { 0, 0, 0, 0, 0, 0 };
            try {
                zz = cc.Split(' ');
            } catch (Exception) { MessageBox.Show("not formath"); return; }
            if (zz.Length != 6) { MessageBox.Show("not formath"); return; }
            try {
                for (int i = 0; i < 6; i++) {
                    xx[i] = Convert.ToInt32(zz[i]);
                }
            } catch (Exception) { MessageBox.Show("not formath"); return; }
            bgr_low = new Bgr(xx[0], xx[2], xx[4]);
            bgr_high = new Bgr(xx[1], xx[3], xx[5]);
            flag_hsv = false;
        }

        private void B_process_Click(object sender, EventArgs e) {
            if (b_process.Text == "True") {
                b_process.Text = "False";
                flag_process = false;
            } else {
                b_process.Text = "True";
                flag_process = true;
            }
        }

        private void H_process_Scroll(object sender, ScrollEventArgs e) {
            s_process.Text = h_process.Value.ToString();
            process_value = h_process.Value;
        }

        private void B_focus_Click(object sender, EventArgs e) {
            SetCapture(Emgu.CV.CvEnum.CapProp.Autofocus, 1);
            try { h_focus.Value = (int)capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Focus); } catch (Exception) { }
            s_focus.Text = h_focus.Value.ToString();
        }

        private void H_focus_Scroll(object sender, ScrollEventArgs e) {
            s_focus.Text = h_focus.Value.ToString();
            SetCapture(Emgu.CV.CvEnum.CapProp.Focus, h_focus.Value);
        }

        private void H_brightness_Scroll(object sender, ScrollEventArgs e) {
            s_brightness.Text = h_brightness.Value.ToString();
            SetCapture(Emgu.CV.CvEnum.CapProp.Brightness, h_brightness.Value);
        }

        private void H_contrast_Scroll(object sender, ScrollEventArgs e) {
            s_contrast.Text = h_contrast.Value.ToString();
            SetCapture(Emgu.CV.CvEnum.CapProp.Contrast, h_contrast.Value);
        }

        private void F1_FormClosed(object sender, FormClosedEventArgs e) {
            //File.WriteAllText("../../config/test_head_" + head + "_zoom_" + steptest + ".txt", h_zoom.Value.ToString());
            //File.WriteAllText("../../config/test_head_" + head + "_pan_" + steptest + ".txt", h_pan.Value.ToString());
            //File.WriteAllText("../../config/test_head_" + head + "_tilt_" + steptest + ".txt", h_tilt.Value.ToString());
            //File.WriteAllText("../../config/test_head_" + head + "_contrast_" + steptest + ".txt", h_contrast.Value.ToString());
            //File.WriteAllText("../../config/test_head_" + head + "_brightness_" + steptest + ".txt", h_brightness.Value.ToString());
            //File.WriteAllText("../../config/test_head_" + head + "_focus_" + steptest + ".txt", h_focus.Value.ToString());
            //File.WriteAllText("../../config/test_head_" + head + "_exposure_" + steptest + ".txt", h_exposure.Value.ToString());
            //File.WriteAllText("../../config/test_head_" + head + "_saturation_" + steptest + ".txt", h_saturation.Value.ToString());
            //File.WriteAllText("../../config/test_head_" + head + "_sharpness_" + steptest + ".txt", h_sharpness.Value.ToString());
            //File.WriteAllText("../../config/test_head_" + head + "_gain_" + steptest + ".txt", h_gain.Value.ToString());
            //File.WriteAllText("../../config/test_head_" + head + "_gamma_" + steptest + ".txt", h_gamma.Value.ToString());
            //File.WriteAllText("../../config/test_head_" + head + "_process_" + steptest + ".txt", h_process.Value.ToString());
            //File.WriteAllText("../../config/test_head_" + head + "_flag_process_" + steptest + ".txt", flag_process.ToString());
            if (steptest_camera_check_led_red_green == true) {
                File.WriteAllText("../../config/test_head_" + head + "_bgr_" + steptest + ".txt", bgr_low.Blue.ToString() + " " +
                                                                                                  bgr_high.Blue.ToString() + " " +
                                                                                                  bgr_low.Green.ToString() + " " +
                                                                                                  bgr_high.Green.ToString() + " " +
                                                                                                  bgr_low.Red.ToString() + " " +
                                                                                                  bgr_high.Red.ToString());
                File.WriteAllText("../../config/test_head_" + head + "_hsv_" + steptest + ".txt", hsv_low.Hue.ToString() + " " +
                                                                                                  hsv_high.Hue.ToString() + " " +
                                                                                                  hsv_low.Satuation.ToString() + " " +
                                                                                                  hsv_high.Satuation.ToString() + " " +
                                                                                                  hsv_low.Value.ToString() + " " +
                                                                                                  hsv_high.Value.ToString());
                File.WriteAllText("../../config/test_head_" + head + "_flag_hsv_" + steptest + ".txt", flag_hsv.ToString());
                File.WriteAllText("../../config/test_head_" + head + "_hsv_mask_" + steptest + ".txt", hsv_mask.ToString());
                File.WriteAllText("../../config/test_head_" + head + "_hsv_timeout_" + steptest + ".txt", hsv_timeout.ToString());
            }
            flag_set_camera = false;
            flag_process_intro = false;
        }

        private void H_tilt_Scroll(object sender, ScrollEventArgs e) {
            s_tilt.Text = h_tilt.Value.ToString();
            SetCapture(Emgu.CV.CvEnum.CapProp.Tilt, h_tilt.Value);
        }

        private void H_pan_Scroll(object sender, ScrollEventArgs e) {
            s_pan.Text = h_pan.Value.ToString();
            SetCapture(Emgu.CV.CvEnum.CapProp.Pan, h_pan.Value);
        }

        private void H_zoom_Scroll(object sender, ScrollEventArgs e) {
            s_zoom.Text = h_zoom.Value.ToString();
            SetCapture(Emgu.CV.CvEnum.CapProp.Zoom, h_zoom.Value);
        }

        private void H_exposure_Scroll(object sender, ScrollEventArgs e) {
            s_exposure.Text = h_exposure.Value.ToString();
            SetCapture(Emgu.CV.CvEnum.CapProp.Exposure, h_exposure.Value);
        }

        private void H_saturation_Scroll(object sender, ScrollEventArgs e) {
            //s_saturation.Text = h_saturation.Value.ToString();
            //SetCapture(Emgu.CV.CvEnum.CapProp.Saturation, h_saturation.Value);
        }

        private void H_sharpness_Scroll(object sender, ScrollEventArgs e) {
            s_sharpness.Text = h_sharpness.Value.ToString();
            SetCapture(Emgu.CV.CvEnum.CapProp.Sharpness, h_sharpness.Value);
        }

        private void H_gain_Scroll(object sender, ScrollEventArgs e) {
            //s_gain.Text = h_gain.Value.ToString();
            //SetCapture(Emgu.CV.CvEnum.CapProp.Gain, h_gain.Value);
        }

        private void H_gamma_Scroll(object sender, ScrollEventArgs e) {
            //s_gamma.Text = h_gamma.Value.ToString();
            //SetCapture(Emgu.CV.CvEnum.CapProp.Gamma, h_gamma.Value);
        }

        private void Show_all_Click(object sender, EventArgs e) {
            if (show_all.Checked) {
                f1.Size = new Size(400, 600);
                h_exposure.Visible = true;
                l_exposure.Visible = true;
                s_exposure.Visible = true;
                h_saturation.Visible = true;
                l_saturation.Visible = true;
                s_saturation.Visible = true;
                h_sharpness.Visible = true;
                l_sharpness.Visible = true;
                s_sharpness.Visible = true;
                h_gain.Visible = true;
                l_gain.Visible = true;
                s_gain.Visible = true;
                h_gamma.Visible = true;
                l_gamma.Visible = true;
                s_gamma.Visible = true;
            } else {
                f1.Size = new Size(400, 400);
                h_exposure.Visible = false;
                l_exposure.Visible = false;
                s_exposure.Visible = false;
                h_saturation.Visible = false;
                l_saturation.Visible = false;
                s_saturation.Visible = false;
                h_sharpness.Visible = false;
                l_sharpness.Visible = false;
                s_sharpness.Visible = false;
                h_gain.Visible = false;
                l_gain.Visible = false;
                s_gain.Visible = false;
                h_gamma.Visible = false;
                l_gamma.Visible = false;
                s_gamma.Visible = false;
            }
        }

        private static void DelaymS(int mS) {
            Stopwatch stopwatchDelaymS = new Stopwatch();
            stopwatchDelaymS.Restart();
            while (mS > stopwatchDelaymS.ElapsedMilliseconds) {
                if (!stopwatchDelaymS.IsRunning)
                    stopwatchDelaymS.Start();
                Application.DoEvents();
            }
            stopwatchDelaymS.Stop();
        }

        bool IsMouseDown = false;
        Point StartLocation;
        Point EndLcation;
        private void pictureBox1_MouseDown(object sender, MouseEventArgs e) {
            if (!flag_set_camera) return;
            if (e.Button != MouseButtons.Left) return;
            IsMouseDown = true;
            StartLocation = e.Location;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e) {
            if (IsMouseDown == true) {
                EndLcation = e.Location;
                pictureBox1.Invalidate();
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e) {
            if (IsMouseDown != true) return;
            if (rect.Size.Width < 30 || rect.Size.Height < 30) return;
            Image<Bgr, byte> imgInput;
            EndLcation = e.Location;
            IsMouseDown = false;
            if (steptest.Contains("read2d")) {
                if (rect != null) {
                    imgInput = img.Copy();
                    imgInput.ROI = rect;
                    Image<Bgr, byte> temp = imgInput.Copy();
                    BarcodeReader reader = new BarcodeReader();
                    Result result = reader.Decode(temp.Bitmap);
                    if (result != null) MessageBox.Show(result.ToString());
                }
            } else if (steptest_camera_matching_lcd_oo_oe_eo_ee == true) {
                if (rect != null) {
                    imgInput = img.Copy();
                    imgInput.ROI = rect;
                    Image<Bgr, byte> temp = imgInput.Copy();
                    temp.Save("../../config/test_head_" + head + "_" + steptest + ".png");
                }
                rect.X -= crop;
                rect.Y -= crop;
                rect.Width = rect.Width + (crop * 2);
                rect.Height = rect.Height + (crop * 2);
                //if (rect.X < 0) rect.X = 0;
                //if (rect.Y < 0) rect.Y = 0;
                //if (rect.Width + rect.X > img.Size.Width) rect.Width = 0;
            } else if (steptest_camera_check_led_red_green == true) {
                if (rect != null) {
                    imgInput = img.Copy();
                    imgInput.ROI = rect;
                    Image<Bgr, byte> temp = imgInput.Copy();
                }
            }
            File.WriteAllText("../../config/test_head_" + head + "_rect_x_" + steptest + ".txt", rect.X.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_rect_y_" + steptest + ".txt", rect.Y.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_rect_width_" + steptest + ".txt", rect.Width.ToString());
            File.WriteAllText("../../config/test_head_" + head + "_rect_height_" + steptest + ".txt", rect.Height.ToString());
            rect_sup = rect;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e) {
            if (rect != null && IsMouseDown == true) {
                e.Graphics.DrawRectangle(Pens.Red, GetRectangle());
            }
        }

        private Rectangle GetRectangle() {
            rect.X = Math.Min(StartLocation.X, EndLcation.X);
            rect.Y = Math.Min(StartLocation.Y, EndLcation.Y);
            rect.Width = Math.Abs(StartLocation.X - EndLcation.X);
            rect.Height = Math.Abs(StartLocation.Y - EndLcation.Y);
            return rect;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e) {
            try
            {
                capture.Dispose();
            } catch { }
        }

        private void setDebugToolStripMenuItem_Click(object sender, EventArgs e) {
            debug = false;
        }

        private void setPortToolStripMenuItem_Click(object sender, EventArgs e) {
            File.WriteAllText("set_port.txt", "set port");
            File.WriteAllText("camera_head_set_port.txt", head);
            capture.Dispose();
            Application.Restart();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            try { capture.Dispose(); } catch { }
            while (true) {
                try {
                    string ff = File.ReadAllText("camera_show_list.txt");
                    File.WriteAllText("camera_show_list.txt", ff.Trim().Replace(head, ""));
                    break;
                } catch { Thread.Sleep(50); continue; }
            }
        }

        private void addStepComparToolStripMenuItem_Click(object sender, EventArgs e) {
            if (!steptest_camera_matching_lcd_oo_oe_eo_ee) return;
            File.WriteAllText("add_step.txt", "True");
            if (!flag_add_step) steptest = steptest + "2";
            else {
                int v = Convert.ToInt32(steptest.Substring(steptest.Length - 1, 1));
                v++;
                steptest = steptest.Substring(0, steptest.Length - 1) + v.ToString();
            }
            File.WriteAllText("../../config/test_head_" + head + "_steptest.txt", steptest);
            Application.Restart();
        }

        private void Frame_height_Click(object sender, EventArgs e) {
            int asd = 1;
            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("_ใส่ frame height\r\ndefault = 800", "frame height", Frame_height.Text, 500, 300);
                if (input == "") return;
                try {
                    asd = Convert.ToInt32(input);
                } catch (Exception) {
                    MessageBox.Show("not format");
                    continue;
                }
                break;
            }
            Frame_height.Text = asd.ToString();
            File.WriteAllText("../../config/test_head_" + head + "_frame_height_" + steptest + ".txt", asd.ToString());
        }

        private void Frame_width_Click(object sender, EventArgs e) {
            int asd = 1;
            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("_ใส่ frame width\r\ndefault = 600", "frame width", Frame_width.Text, 500, 300);
                if (input == "") return;
                try {
                    asd = Convert.ToInt32(input);
                } catch (Exception) {
                    MessageBox.Show("not format");
                    continue;
                }
                break;
            }
            Frame_width.Text = asd.ToString();
            File.WriteAllText("../../config/test_head_" + head + "_frame_width_" + steptest + ".txt", asd.ToString());
        }

        private void Process_timeout__Click(object sender, EventArgs e) {
            int asd = 1;
            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("_ใส่ process timeout\r\ndefault = 500", "process timeout", Process_timeout_.Text, 500, 300);
                if (input == "") return;
                try {
                    asd = Convert.ToInt32(input);
                } catch (Exception) {
                    MessageBox.Show("not format");
                    continue;
                }
                break;
            }
            Process_timeout_.Text = asd.ToString();
            File.WriteAllText("../../config/test_head_" + head + "_process_timeout_" + steptest + ".txt", asd.ToString());
        }

        private void Process_roi__Click(object sender, EventArgs e) {
            int asd = 1;
            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("_ใส่ process roi\r\ndefault = 10", "process roi", Process_roi_.Text, 500, 300);
                if (input == "") return;
                try {
                    asd = Convert.ToInt32(input);
                } catch (Exception) {
                    MessageBox.Show("not format");
                    continue;
                }
                break;
            }
            Process_roi_.Text = asd.ToString();
            File.WriteAllText("../../config/test_head_" + head + "_process_roi_" + steptest + ".txt", asd.ToString());
        }

        private void Process_scale_limit__Click(object sender, EventArgs e) {
            int asd = 1;
            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("_ใส่ process scale limit\r\ndefault = 40", "process scale limit", Process_scale_limit_.Text, 500, 300);
                if (input == "") return;
                try {
                    asd = Convert.ToInt32(input);
                } catch (Exception) {
                    MessageBox.Show("not format");
                    continue;
                }
                break;
            }
            Process_scale_limit_.Text = asd.ToString();
            File.WriteAllText("../../config/test_head_" + head + "_process_scale_limit_" + steptest + ".txt", asd.ToString());
        }

        private void Process_scale_next__Click(object sender, EventArgs e) {
            int asd = 1;
            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("_ใส่ process scale next\r\ndefault = 2", "process scale next", Process_scale_next_.Text, 500, 300);
                if (input == "") return;
                try {
                    asd = Convert.ToInt32(input);
                } catch (Exception) {
                    MessageBox.Show("not format");
                    continue;
                }
                break;
            }
            Process_scale_next_.Text = asd.ToString();
            File.WriteAllText("../../config/test_head_" + head + "_process_scale_next_" + steptest + ".txt", asd.ToString());
        }

        private void onebyone_Click(object sender, EventArgs e) {
            File.WriteAllText("../../config/camera_show_onebyone" + ".txt", onebyone.Checked.ToString());
        }

        private void process_roi_set_Click(object sender, EventArgs e) {
            File.WriteAllText("../../config/camera_show_process_roi_set" + ".txt", process_roi_set.Checked.ToString());
        }

        private void ctms_focusAutoTrue_Click(object sender, EventArgs e) {
            ctms_focusAutoTrue.Checked = true;
            ctms_focusAutoFalse.Checked = false;
            File.WriteAllText("../../config/camera_show_process_autoFocus" + ".txt", true.ToString());
            flagAutoFocus = true;
        }
        private void ctms_focusAutoFalse_Click(object sender, EventArgs e) {
            ctms_focusAutoTrue.Checked = false;
            ctms_focusAutoFalse.Checked = true;
            File.WriteAllText("../../config/camera_show_process_autoFocus" + ".txt", false.ToString());
            flagAutoFocus = false;
        }
        private void ctms_digitSn_Click(object sender, EventArgs e) {
            int result = 1;

            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Keys Digit SN\r\nDefault = 13", "Digit SN", ctms_digitSn.Text, 500, 300);
                if (input == "") return;
                try {
                    result = Convert.ToInt32(input);
                } catch {
                    MessageBox.Show("not format");
                    continue;
                }
                break;
            }
            digitSn = result;
            ctms_digitSn.Text = result.ToString();
            File.WriteAllText("../../config/test_head_" + head + "_digitSn_" + steptest + ".txt", result.ToString());
        }
        private void ctms_setDegree_Click(object sender, EventArgs e) {
            int result = 1;

            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Keys Degree\r\nDefault = 0", "Degree", ctms_setDegree.Text, 500, 300);
                if (input == "") return;
                try {
                    result = Convert.ToInt32(input);
                } catch {
                    MessageBox.Show("not format");
                    continue;
                }
                break;
            }
            degreeSet = result;
            ctms_setDegree.Text = result.ToString();
            File.WriteAllText("../../config/test_head_" + head + "_degree_" + steptest + ".txt", result.ToString());
        }
        private void ctms_setGammaAddressCsv_Click(object sender, EventArgs e) {
            string address = String.Empty;
            while (true) {
                string input = Microsoft.VisualBasic.Interaction.InputBox("Set gamma address in csv", "Gamma Address",
                    ctms_setGammaAddressCsv.Text, 500, 300);
                if (input == "") return;
                if (!CheckPasswordSetAddress()) {
                    return;
                }
                address = input;
                break;
            }
            ctms_setGammaAddressCsv.Text = address;
            File.WriteAllText("../../config/test_head_" + head + "_gamma_" + steptest + ".txt", address.ToString());
        }
        private void ctms_propertySetting_Click(object sender, EventArgs e) {
            if (!CheckPasswordSetAddress()) {
                return;
            }
            SetCapture(CapProp.Settings, 0);
        }
        private void ctms_setGammaAddress_Click(object sender, EventArgs e) {
            if (!CheckPasswordSetAddress())
            {
                return;
            }
            ctms_setGammaAddress.Checked = true;
            ctms_usePort.Checked = false;
            if (steptest_camera_read2d_flag)
            {
                File.WriteAllText("../../config/test_head_" + head + "_address_read2d.txt", Address.gamma);
            }
            else
            {
                File.WriteAllText("../../config/test_head_" + head + "_address.txt", Address.gamma);
            }
        }
        private void ctms_saveConfig_Click(object sender, EventArgs e) {
            if (!CheckPasswordSetAddress())
            {
                return;
            }
            ReadConfigCameraToCsvFile();
        }
        private void ctms_usePort_Click(object sender, EventArgs e) {
            if (!CheckPasswordSetAddress())
            {
                return;
            }
            ctms_setGammaAddress.Checked = false;
            ctms_usePort.Checked = true;
            if (steptest_camera_read2d_flag)
            {
                File.WriteAllText("../../config/test_head_" + head + "_address_read2d.txt", Address.port);
            }
            else
            {
                File.WriteAllText("../../config/test_head_" + head + "_address.txt", Address.port);
            }
        }
        public static class Address {
            public static readonly string gamma = "Gamma";
            public static readonly string port = "Port";
        }
    }
}
