import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import { STYLE_PRESET_LIST, StylePreset } from "../../core/theme/stylePresets";
import { NavLayout, ThemeModePreference, ThemeService } from "../../core/theme/theme.service";
import { SurfaceCard } from "../../shared/ui/surface-card";

const MODE_OPTIONS: { value: ThemeModePreference; label: string; hint: string; icon: string }[] = [
  { value: "light", label: "Chiaro", hint: "Sempre chiaro", icon: "light_mode" },
  { value: "dark", label: "Scuro", hint: "Sempre scuro", icon: "dark_mode" },
  { value: "system", label: "Automatico", hint: "Segue il sistema operativo", icon: "brightness_medium" },
];

const NAV_OPTIONS: { value: NavLayout; label: string; hint: string }[] = [
  { value: "sidebar", label: "Barra laterale", hint: "Menu sempre visibile a sinistra, comprimibile" },
  { value: "topbar", label: "Barra superiore", hint: "Più spazio orizzontale per grafici e tabelle" },
];

/** Impostazioni → Aspetto: stile (con anteprima disegnata dai token), tema e layout di navigazione. */
@Component({
  selector: "app-appearance-settings",
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SurfaceCard, MatIconModule],
  templateUrl: "./appearance-settings.html",
  styleUrl: "./appearance-settings.scss",
})
export class AppearanceSettings {
  protected readonly theme = inject(ThemeService);
  protected readonly presets = STYLE_PRESET_LIST;
  protected readonly modeOptions = MODE_OPTIONS;
  protected readonly navOptions = NAV_OPTIONS;
  protected readonly previewBars = [40, 70, 55, 90, 65];

  /** token dello stile nella modalità corrente, per l'anteprima */
  protected tokensOf(preset: StylePreset) {
    return preset[this.theme.mode()];
  }
}
