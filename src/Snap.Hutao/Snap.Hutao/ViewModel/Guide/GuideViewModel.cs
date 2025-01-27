﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Notifications;
using Microsoft.Windows.AppLifecycle;
using Snap.Hutao.Core;
using Snap.Hutao.Core.Setting;
using Snap.Hutao.Model;
using Snap.Hutao.Service;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Snap.Hutao.ViewModel.Guide;

/// <summary>
/// 指引视图模型
/// </summary>
[ConstructorGenerated]
[Injection(InjectAs.Singleton)]
internal sealed partial class GuideViewModel : Abstraction.ViewModel
{
    private readonly IServiceProvider serviceProvider;
    private readonly ITaskContext taskContext;
    private readonly AppOptions appOptions;
    private readonly RuntimeOptions runtimeOptions;

    private string nextOrCompleteButtonText = SH.ViewModelGuideActionNext;
    private bool isNextOrCompleteButtonEnabled = true;
    private NameValue<string>? selectedCulture;
    private bool isTermOfServiceAgreed;
    private bool isPrivacyPolicyAgreed;
    private bool isIssueReportAgreed;
    private bool isOpenSourceLicenseAgreed;
    private ObservableCollection<DownloadSummary>? downloadSummaries;

    public uint State
    {
        get
        {
            uint value = LocalSetting.Get(SettingKeys.Major1Minor7Revision0GuideState, 0U);
            GuideState state = (GuideState)value;

            if (state is GuideState.Document)
            {
                IsTermOfServiceAgreed = false;
                IsPrivacyPolicyAgreed = false;
                IsIssueReportAgreed = false;
                IsOpenSourceLicenseAgreed = false;
                (NextOrCompleteButtonText, IsNextOrCompleteButtonEnabled) = (SH.ViewModelGuideActionNext, false);
            }
            else if (state is GuideState.StaticResourceBegin)
            {
                (NextOrCompleteButtonText, IsNextOrCompleteButtonEnabled) = (SH.ViewModelGuideActionStaticResourceBegin, false);
                DownloadStaticResourceAsync().SafeForget();
            }
            else if (state is GuideState.Completed)
            {
                (NextOrCompleteButtonText, IsNextOrCompleteButtonEnabled) = (SH.ViewModelGuideActionComplete, true);
            }
            else
            {
                (NextOrCompleteButtonText, IsNextOrCompleteButtonEnabled) = (SH.ViewModelGuideActionNext, true);
            }

            return value;
        }

        set
        {
            LocalSetting.Set(SettingKeys.Major1Minor7Revision0GuideState, value);
            OnPropertyChanged();
        }
    }

    public string NextOrCompleteButtonText { get => nextOrCompleteButtonText; set => SetProperty(ref nextOrCompleteButtonText, value); }

    public bool IsNextOrCompleteButtonEnabled { get => isNextOrCompleteButtonEnabled; set => SetProperty(ref isNextOrCompleteButtonEnabled, value); }

    public AppOptions AppOptions { get => appOptions; }

    public RuntimeOptions RuntimeOptions { get => runtimeOptions; }

    public NameValue<string>? SelectedCulture
    {
        get => selectedCulture ??= AppOptions.Cultures.FirstOrDefault(c => c.Value == AppOptions.CurrentCulture.Name);
        set
        {
            if (SetProperty(ref selectedCulture, value) && value is not null)
            {
                AppOptions.CurrentCulture = CultureInfo.GetCultureInfo(value.Value);
                ++State;
                AppInstance.Restart(string.Empty);
            }
        }
    }

    public bool IsTermOfServiceAgreed
    {
        get => isTermOfServiceAgreed; set
        {
            if (SetProperty(ref isTermOfServiceAgreed, value))
            {
                OnAgreeSateChanged();
            }
        }
    }

    public bool IsPrivacyPolicyAgreed
    {
        get => isPrivacyPolicyAgreed; set
        {
            if (SetProperty(ref isPrivacyPolicyAgreed, value))
            {
                OnAgreeSateChanged();
            }
        }
    }

    public bool IsIssueReportAgreed
    {
        get => isIssueReportAgreed; set
        {
            if (SetProperty(ref isIssueReportAgreed, value))
            {
                OnAgreeSateChanged();
            }
        }
    }

    public bool IsOpenSourceLicenseAgreed
    {
        get => isOpenSourceLicenseAgreed; set
        {
            if (SetProperty(ref isOpenSourceLicenseAgreed, value))
            {
                OnAgreeSateChanged();
            }
        }
    }

    /// <summary>
    /// 下载信息
    /// </summary>
    public ObservableCollection<DownloadSummary>? DownloadSummaries { get => downloadSummaries; set => SetProperty(ref downloadSummaries, value); }

    protected override ValueTask<bool> InitializeUIAsync()
    {
        return ValueTask.FromResult(true);
    }

    [Command("NextOrCompleteCommand")]
    private void NextOrComplete()
    {
        ++State;
    }

    private void OnAgreeSateChanged()
    {
        IsNextOrCompleteButtonEnabled = IsTermOfServiceAgreed && IsPrivacyPolicyAgreed && IsIssueReportAgreed && IsOpenSourceLicenseAgreed;
    }

    private async ValueTask DownloadStaticResourceAsync()
    {
        IEnumerable<DownloadSummary> downloadSummaries = GenerateStaticResourceDownloadTasks();

        DownloadSummaries = downloadSummaries.ToObservableCollection();

        await Parallel.ForEachAsync(downloadSummaries, async (summary, token) =>
        {
            if (await summary.DownloadAndExtractAsync().ConfigureAwait(false))
            {
                taskContext.InvokeOnMainThread(() => DownloadSummaries.Remove(summary));
            }
        }).ConfigureAwait(false);

        StaticResource.FulfillAllContracts();

        LocalSetting.Set(SettingKeys.Major1Minor7Revision0GuideState, (uint)GuideState.Completed);
        AppInstance.Restart(string.Empty);
    }

    private IEnumerable<DownloadSummary> GenerateStaticResourceDownloadTasks()
    {
        Dictionary<string, DownloadSummary> downloadSummaries = new();

        if (StaticResource.IsContractUnfulfilled(StaticResource.V1Contract))
        {
            downloadSummaries.TryAdd("Bg", new(serviceProvider, "Bg"));
            downloadSummaries.TryAdd("AvatarIcon", new(serviceProvider, "AvatarIcon"));
            downloadSummaries.TryAdd("GachaAvatarIcon", new(serviceProvider, "GachaAvatarIcon"));
            downloadSummaries.TryAdd("GachaAvatarImg", new(serviceProvider, "GachaAvatarImg"));
            downloadSummaries.TryAdd("EquipIcon", new(serviceProvider, "EquipIcon"));
            downloadSummaries.TryAdd("GachaEquipIcon", new(serviceProvider, "GachaEquipIcon"));
            downloadSummaries.TryAdd("NameCardPic", new(serviceProvider, "NameCardPic"));
            downloadSummaries.TryAdd("Skill", new(serviceProvider, "Skill"));
            downloadSummaries.TryAdd("Talent", new(serviceProvider, "Talent"));
        }

        if (StaticResource.IsContractUnfulfilled(StaticResource.V2Contract))
        {
            downloadSummaries.TryAdd("AchievementIcon", new(serviceProvider, "AchievementIcon"));
            downloadSummaries.TryAdd("ItemIcon", new(serviceProvider, "ItemIcon"));
            downloadSummaries.TryAdd("IconElement", new(serviceProvider, "IconElement"));
            downloadSummaries.TryAdd("RelicIcon", new(serviceProvider, "RelicIcon"));
        }

        if (StaticResource.IsContractUnfulfilled(StaticResource.V3Contract))
        {
            downloadSummaries.TryAdd("Skill", new(serviceProvider, "Skill"));
            downloadSummaries.TryAdd("Talent", new(serviceProvider, "Talent"));
        }

        if (StaticResource.IsContractUnfulfilled(StaticResource.V4Contract))
        {
            downloadSummaries.TryAdd("AvatarIcon", new(serviceProvider, "AvatarIcon"));
        }

        if (StaticResource.IsContractUnfulfilled(StaticResource.V5Contract))
        {
            downloadSummaries.TryAdd("MonsterIcon", new(serviceProvider, "MonsterIcon"));
        }

        return downloadSummaries.Select(x => x.Value);
    }
}