﻿//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Pronunciation.Core.Database
{
    using System;
    using System.Collections.Generic;

    public partial class ExerciseAudio
    {
        public Guid AudioId { get; set; }
        public byte[] RawData { get; set; }
        public string AudioName { get; set; }
        public Guid ExerciseId { get; set; }

        public virtual Exercise Exercise { get; set; }
    }
}