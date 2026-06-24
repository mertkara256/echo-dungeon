# Echo Dungeon

Echo Dungeon, Unity ile geliştirilmiş oda tabanlı bir aksiyon dungeon oyunudur. Oyuncu odaları temizleyerek ilerler, coin toplar, shop ve upgrade sistemleriyle karakterini güçlendirir ve boss karşılaşmasına hazırlanır.

## Özellikler

- Oda tabanlı dungeon ilerleyişi
- Combat, treasure, upgrade ve boss oda tipleri
- Melee, ranged, tank ve elite ranged düşman arketipleri
- Corrupted Guardian boss karşılaşması
- Coin, shop, locked chest ve upgrade station sistemleri
- Gün ilerleyişi ve run state yönetimi
- Stasis ve poison alan büyüleri
- HUD, can barı, spell cooldown ve oda ilerleme UI'ı
- Universal Render Pipeline kullanımı

## Teknoloji

- Unity `6000.3.10f1`
- Universal Render Pipeline `17.3.0`
- Unity Input System `1.18.0`
- AI Navigation `2.0.10`
- TextMesh Pro
- DOTween

## Projeyi Çalıştırma

1. Unity Hub üzerinden Unity `6000.3.10f1` sürümünü kurun.
2. Bu repository'yi klonlayın:

   ```bash
   git clone https://github.com/mertkara256/Echo-Dungeon.git
   ```

3. Unity Hub'da `Add project from disk` seçeneğiyle proje klasörünü açın.
4. Unity paketleri restore edene kadar bekleyin.
5. `Assets/Scenes/MainMenu.unity` veya `Assets/Scenes/SampleScene.unity` sahnesini açın.
6. Play tuşuna basarak oyunu başlatın.

## Kontroller

| Aksiyon | Tuş |
| --- | --- |
| Hareket | `WASD` veya yön tuşları |
| Saldırı | Sol mouse |
| İkincil saldırı | Sağ mouse |
| Etkileşim | `E` |
| Dash | `Left Shift` veya `Right Shift` |
| Potion | `Q` |
| Speed boost / seçim 1 | `1` |
| Stasis spell / seçim 2 | `2` |
| Poison spell / seçim 3 | `3` |
| Pause | `Esc` |
| Restart | `R` |

Debug kısayolları:

- `K`: günü ilerletir
- `L`: Echo Barrage özelliğini verir

## Proje Yapısı

```text
Assets/
  Animations/       Player animasyonları
  Materials/        Oyun materyalleri
  Plugins/          DOTween ve üçüncü parti pluginler
  Prefabs/          Boss, dungeon itemları ve düşman prefabları
  Scenes/           MainMenu ve SampleScene sahneleri
  Scripts/          Oyun kodları
  Settings/         URP ve render ayarları
Packages/           Unity package manifest ve lock dosyaları
ProjectSettings/    Unity proje ayarları
ReportOutput/       SE330 rapor çıktıları ve ekran görüntüleri
```

## Önemli Script Klasörleri

- `Assets/Scripts/Core`: game state, input, event ve skor yönetimi
- `Assets/Scripts/Dungeon`: oda, kapı, shop, chest, item ve room manager sistemleri
- `Assets/Scripts/Enemy`: düşman davranışları ve coin drop sistemi
- `Assets/Scripts/Player`: oyuncu hareket, combat, can, inventory, stat ve spell sistemleri
- `Assets/Scripts/Boss`: boss kontrolü, fazları, canı ve hazard alanları
- `Assets/Scripts/UI`: HUD, menü ve oyun içi UI bileşenleri

## Git Notları

Bu repository sadece kaynak proje dosyalarını takip eder. Unity'nin yerel olarak ürettiği `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `.plastic/`, `.csproj` ve `.sln` gibi dosyalar `.gitignore` ile dışarıda bırakılmıştır.

## Rapor

SE330 teslim çıktıları `ReportOutput/` klasöründe tutulur. Bu klasörde proje raporu, render edilmiş PDF, ekran görüntüleri ve paket/assets teslim arşivi bulunur.
