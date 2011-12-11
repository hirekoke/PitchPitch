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
 *   - add methods to get key name from Guid / get Guid from key name
 * 
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace CoreAudioApi
{
    public static class PKEY 
    {
        public static readonly Guid PKEY_DeviceInterface_FriendlyName = new Guid( 0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0);  
        public static readonly Guid PKEY_AudioEndpoint_FormFactor = new Guid( 0x1da5d803, 0xd492, 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e); 
        public static readonly Guid PKEY_AudioEndpoint_ControlPanelPageProvider = new Guid( 0x1da5d803, 0xd492, 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e); 
        public static readonly Guid PKEY_AudioEndpoint_Association = new Guid( 0x1da5d803, 0xd492, 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e);
        public static readonly Guid PKEY_AudioEndpoint_PhysicalSpeakers = new Guid( 0x1da5d803, 0xd492, 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e);
        public static readonly Guid PKEY_AudioEndpoint_GUID = new Guid( 0x1da5d803, 0xd492, 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e);
        public static readonly Guid PKEY_AudioEndpoint_Disable_SysFx = new Guid( 0x1da5d803, 0xd492, 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e);
        public static readonly Guid PKEY_AudioEndpoint_FullRangeSpeakers = new Guid( 0x1da5d803, 0xd492, 0x4edd, 0x8c, 0x23, 0xe0, 0xc0, 0xff, 0xee, 0x7f, 0x0e);
        public static readonly Guid PKEY_AudioEngine_DeviceFormat = new Guid(0xf19f064d, 0x82c, 0x4e27, 0xbc, 0x73, 0x68, 0x82, 0xa1, 0xbb, 0x8e, 0x4c);

        /* added -> */
        private static Dictionary<Guid, string> _keyDic = null;
        private static void createDic()
        {
            _keyDic = new Dictionary<Guid, string>();
            _keyDic.Add(PKEY_AudioEndpoint_Association, "AudioEndpoint_Association");
            _keyDic.Add(PKEY_AudioEndpoint_ControlPanelPageProvider, "AudioEndpoint_ControlPanelPageProvider");
            _keyDic.Add(PKEY_AudioEndpoint_Disable_SysFx, "AudioEndpoint_Disable_SysFx");
            _keyDic.Add(PKEY_AudioEndpoint_FormFactor, "AudioEndpoint_FormFactor");
            _keyDic.Add(PKEY_AudioEndpoint_FullRangeSpeakers, "AudioEndpoint_FullRangeSpeakers");
            _keyDic.Add(PKEY_AudioEndpoint_GUID, "AudioEndpoint_GUID");
            _keyDic.Add(PKEY_AudioEndpoint_PhysicalSpeakers, "AudioEndpoint_PhysicalSpeakers");
            _keyDic.Add(PKEY_AudioEngine_DeviceFormat, "AudioEngine_DeviceFormat");
            _keyDic.Add(PKEY_DeviceInterface_FriendlyName, "DeviceInterface_FriendlyName");
        }

        public static string GetKeyName(Guid guid)
        {
            if (_keyDic == null) createDic();
            if (_keyDic.ContainsKey(guid))
            {
                return _keyDic[guid];
            }
            else
            {
                return "Unknown";
            }
        }
        public static Guid GetGuid(string keyName)
        {
            if (_keyDic == null) createDic();
            foreach (KeyValuePair<Guid, string> kv in _keyDic)
            {
                if (keyName == kv.Value) return kv.Key;
            }
            return Guid.Empty;
        }
        /* <- added */
    }
}
