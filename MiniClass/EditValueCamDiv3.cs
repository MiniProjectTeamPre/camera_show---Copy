using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace camera_show.MiniClass {
    public class EditValueCamDiv3 {

        public EditValueCamDiv3(Form1 main_) {
            main = main_;
        }

        private Form1 main;

        public int time;

        public bool state1;

        public bool state2;

        public Stopwatch stopwatch = new Stopwatch();

        public void Process() {
            if (main.timeout.ElapsedMilliseconds >= time & main.timeout.ElapsedMilliseconds < (time * 2) & !state1)
            {
                state1 = true;
                int iFocus = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + main.head + "_focus_" + main.steptest + ".txt"));
                int iExposure = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + main.head + "_exposure_" + main.steptest + ".txt"));
                main.SetCapture(CapProp.Focus, iFocus - 1);
                main.SetCapture(CapProp.Exposure, iExposure - 1);
                stopwatch.Restart();
            }
            if (stopwatch.ElapsedMilliseconds >= 1000 & !state2)
            {
                state2 = true;
                int iFocus = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + main.head + "_focus_" + main.steptest + ".txt"));
                int iExposure = Convert.ToInt32(File.ReadAllText("../../config/test_head_" + main.head + "_exposure_" + main.steptest + ".txt"));
                main.SetCapture(CapProp.Focus, iFocus);
                main.SetCapture(CapProp.Exposure, iExposure);
            }
        }
    }
}
