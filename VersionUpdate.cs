using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace camera_show {
    internal class VersionUpdate {
        //V2022.02
        //Edit set gamma to address

        //V2022.03
        //Edit set gamma and gain to address

        //V2022.04
        //Add saturation to address

        //V2022.05
        //Remove saturation address
        //Add use port

        //V2023.01
        //Remove auto focus in check image and check led

        //V2023.02
        //Edit path log error CameraSetCapProp

        //V2023.03
        //Add log program catch all process

        //V2023.04
        //Edit bug select address use port but show select gain

        //V2023.05 Copy
        //ลองเทสแล้วต้องใช้คู่กันถึงจะใช้ได้ทั้งตอนรันโค้ดและรัน exe อันล่างให้เอาไปใส่ใน Program.cs
        //AppDomain.CurrentDomain.UnhandledException 
        //Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);

        //V2023.06
        //Remove address gain

        //V2023.07
        //Add edit value cam div3

        //V2023.08
        //ถ้ามันหา port ไม่เจอ ให้ตีเฟวเลย ไม่ต้องเปิด port 0 ถ้าเป็นดีบัคโหมดให้เปิด port 0 เหมือนเดิม

        //V2023.09
        //Edit image icon

        //V2023.10
        //เปลี่ยนวิธีการปรับกล้องใหม่ ตอนแรกเอา timeout มาหาร 3 แล้วแบ่งเป็น 2 ช่วง
        //ก็คือเปลี่ยนตอนช่วง 2 ให้นับต่อจากช่วงแรงไป 1 วินาที แล้วเข้าช่วง 2 ได้เลย
    }
}
