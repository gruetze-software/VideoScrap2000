using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace VideoScrap2000.ViewModels;

public partial class MainViewModel
{
    private HashSet<int>? actorMovieIds;
    private int? actorFilterTmdbId;
    private int actorFilterVersion;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasActorFilter))]
    private string actorFilterName = "";
    public bool HasActorFilter => ActorFilterName.Length > 0;
    public string ActorFilterTitle => Localization.Locale.T("Darsteller");
    public string ClearActorFilterTitle => Localization.Locale.T("Darstellerfilter aufheben");

    public async Task FilterByActorAsync(ActorEditor actor)
    {
        var version = ++actorFilterVersion;
        var name = actor.Name.Trim();
        if (name.Length == 0) return;
        try
        {
            var ids = await metadata.FindMovieIdsByActorAsync(name, actor.TmdbId, CancellationToken.None);
            if (version != actorFilterVersion) return;
            actorMovieIds = ids;
            actorFilterTmdbId = actor.TmdbId;
            ActorFilterName = name;
            Filter();
        }
        catch (Exception ex) { if (version == actorFilterVersion) Status = ex.Message; }
    }

    private async Task RefreshActorFilterAsync(CancellationToken token)
    {
        if (!HasActorFilter) return;
        var version = actorFilterVersion;
        var ids = await metadata.FindMovieIdsByActorAsync(ActorFilterName, actorFilterTmdbId, token);
        if (version == actorFilterVersion) actorMovieIds = ids;
    }

    [RelayCommand]
    private void ClearActorFilter()
    {
        actorFilterVersion++;
        actorMovieIds = null;
        actorFilterTmdbId = null;
        ActorFilterName = "";
        Filter();
    }
}
