using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CoreAudioApi.Interfaces
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct KSDATAFORMAT
    {
        /// <summary>
        /// Specifies the size, in bytes, of the KSDATAFORMAT structure.
        /// </summary>
        public UInt32 FormatSize;

        /// <summary>
        /// Reserved, must be set to zero.
        /// </summary>
        public UInt32 Flags;

        /// <summary>
        /// Specifies the sample size of the data, for fixed sample sizes, or zero, if the format has a variable sample size.
        /// </summary>
        public UInt32 SampleSize;

        /// <summary>
        /// Reserved for system use. Drivers must set this member to zero.
        /// </summary>
        public UInt32 Reserved;

        /// <summary>
        /// Specifies the general format type.
        /// </summary>
        public Guid MajorFormat;

        /// <summary>
        /// Specifies the subformat of a general format type.
        /// </summary>
        public Guid SubFormat;

        /// <summary>
        /// Specifies additional data format type information.
        /// </summary>
        public Guid Specifier;
    }
}
