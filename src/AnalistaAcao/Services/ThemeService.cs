namespace AnalistaAcao.Services;

public interface IThemeService
{
    /// <summary>Tema salvo (Unspecified = seguir o sistema).</summary>
    AppTheme SavedTheme { get; }

    /// <summary>Aplica o tema imediatamente e persiste a escolha.</summary>
    void Apply(AppTheme theme);
}

/// <summary>
/// Alterna entre Claro/Escuro/Sistema usando <see cref="Application.UserAppTheme"/>.
/// Todas as cores da UI usam AppThemeBinding, então a troca é instantânea.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private const string PreferenceKey = "app_theme";

    public AppTheme SavedTheme =>
        (AppTheme)Preferences.Default.Get(PreferenceKey, (int)AppTheme.Unspecified);

    public void Apply(AppTheme theme)
    {
        Preferences.Default.Set(PreferenceKey, (int)theme);
        if (Application.Current is not null)
            Application.Current.UserAppTheme = theme;
    }
}
