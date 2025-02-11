// Decompiled with JetBrains decompiler
// Type: vn_matchstock.Market
// Assembly: vn_matchstock, Version=2023.10.31.237, Culture=neutral, PublicKeyToken=null
// MVID: F04EE2D5-AF78-4C81-9699-CA665C6E41C5
// Assembly location: /Users/tunghaotu/www/service/vn_matchstock/vn_matchstock.dll

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#nullable enable
namespace vn_matchstock
{
    // [RequiredMember]
    public class Market
    {
        // [RequiredMember]
        public required string name { get; set; }

        public bool is_allowed_daytrading { get; set; } = false;

        public int interval { get; set; } = 500;

        public TimeSpan end_time { get; set; }

        // [RequiredMember]
        public required List<Exchange> exchanges { get; set; }

        // [Obsolete("Constructors of types with required members are not supported in this version of your compiler.", true)]
        // [CompilerFeatureRequired("RequiredMembers")]
        public Market()
        {
        }
    }
}