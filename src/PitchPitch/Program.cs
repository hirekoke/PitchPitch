using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;

namespace PitchPitch
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                PitchPitch = new PitchPitch();
                PitchPitch.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.WindowTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.Instance.Log(LogType.Error, ex.Message);
                Logger.Instance.Log(LogType.Info, ex.StackTrace);
            }
        }

        public static PitchPitch PitchPitch;
    }
}
