﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Snap.Hutao.Core.Abstraction;
using Snap.Hutao.Core.Database;
using Snap.Hutao.Core.ExceptionService;
using Snap.Hutao.Service.GachaLog;
using Snap.Hutao.Web.Hoyolab.Hk4e.Event.GachaInfo;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Snap.Hutao.Model.Entity;

/// <summary>
/// 祈愿记录存档
/// </summary>
[HighQuality]
[Table("gacha_archives")]
internal sealed partial class GachaArchive : ISelectable, IMappingFrom<GachaArchive, string>
{
    /// <summary>
    /// 内部Id
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid InnerId { get; set; }

    /// <summary>
    /// 记录的Uid
    /// </summary>
    public string Uid { get; set; } = default!;

    /// <inheritdoc/>
    public bool IsSelected { get; set; }

    /// <summary>
    /// 构造一个新的卡池存档
    /// </summary>
    /// <param name="uid">uid</param>
    /// <returns>新的卡池存档</returns>
    public static GachaArchive From(string uid)
    {
        return new() { Uid = uid };
    }
}