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

    public partial class WordCategoryMembership
    {
        public System.Guid MembershipId { get; set; }
        public System.Guid WordCategoryId { get; set; }
        public string WordName { get; set; }

        public virtual WordCategory WordCategory { get; set; }
    }
}
