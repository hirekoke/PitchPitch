/*
  LICENSE
  -------
  Copyright (C) 2007-2010 Ray Molenkamp

  This source code is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this source code or the software it produces.

  Permission is granted to anyone to use this source code for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this source code must not be misrepresented; you must not
     claim that you wrote the original source code.  If you use this source code
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original source code.
  3. This notice may not be removed or altered from any source distribution.
*/
/*
 * modified by hirekoke
 *   - implemented some methods
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;
using CoreAudioApi.Interfaces;
using System.Runtime.InteropServices;

namespace CoreAudioApi
{
    public class AudioSessionManager
    {
        private IAudioSessionManager2 _AudioSessionManager;
        private SessionCollection _Sessions;
        
        internal AudioSessionManager(IAudioSessionManager2 realAudioSessionManager)
        {
            _AudioSessionManager = realAudioSessionManager;
            IAudioSessionEnumerator _SessionEnum ;
            Marshal.ThrowExceptionForHR(_AudioSessionManager.GetSessionEnumerator(out _SessionEnum));
            _Sessions = new SessionCollection(_SessionEnum);
        }

        public SessionCollection Sessions
        {
            get
            {
                return _Sessions;
            }
        }

        /* added -> */
        public AudioSessionControl GetAudioSessionControl(Guid AudioSessionGuid, bool CrossProcessSession)
        {
            IAudioSessionControl2 result = null;
            int hr = _AudioSessionManager.GetAudioSessionControl(ref AudioSessionGuid, (uint)(CrossProcessSession ? 1 : 0), out result);
            Marshal.ThrowExceptionForHR(hr);
            if (result == null) return null;
            return new AudioSessionControl(result);
        }
        /* <- added */
    }
}
