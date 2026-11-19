# Kenney Art Packs

The kits' sample games use free art packs by Kenney (kenney.nl). Every pack is **CC0**: it can be used in any game, including commercial ones, without credit. The course credits Kenney anyway, in `Assets/_Course/Credits/Course Credits.txt`, which the game's Credits screen shows.

## Packs by kit

| Kit | Pack | Download |
|---|---|---|
| Platformer | Pixel Platformer | https://kenney.nl/assets/pixel-platformer |
| Top-down adventure | Tiny Dungeon | https://kenney.nl/assets/tiny-dungeon (also https://kenney-assets.itch.io/tiny-dungeon) |
| Top-down adventure | Tiny Town (outdoor areas) | https://kenney.nl/assets/tiny-town |
| Turn-based battle | Tiny Dungeon (the same pack as the adventure kit) | https://kenney.nl/assets/tiny-dungeon |
| Arcade classic: Breakout and Pong | Puzzle Pack | https://kenney.nl/assets/puzzle-pack |
| Arcade classic: Asteroids | Space Shooter Redux | https://kenney.nl/assets/space-shooter-redux |
| Physics puzzle | Physics Assets | https://kenney.nl/assets/physics-assets (also https://opengameart.org/content/physics-assets) |
| Top-down shooter | Pixel Shmup, or Space Shooter Redux | https://kenney.nl/assets/pixel-shmup |
| Endless runner | Pixel Platformer (the same pack as the platformer kit) | https://kenney.nl/assets/pixel-platformer |
| Grid puzzle | Sokoban | https://kenney.nl/assets/sokoban |
| Tower defense | Tower Defense (top-down) | https://kenney.nl/assets/tower-defense-top-down |
| Card game | Playing Cards Pack | https://kenney.nl/assets/playing-cards-pack |
| Management and strategy, farming and life sim | Tiny Town, with the UI Pack | https://kenney.nl/assets/tiny-town |
| Menus and HUDs for any kit | UI Pack | https://kenney.nl/assets/ui-pack (also https://opengameart.org/content/ui-pack) |
| 3D kits | Platformer Kit, Prototype Textures | https://kenney.nl/assets/platformer-kit, https://kenney.nl/assets/prototype-textures |

## Sound packs

| Use | Pack | Download |
|---|---|---|
| Menu clicks and UI sounds | Interface Sounds | https://kenney.nl/assets/interface-sounds |
| Hits, impacts, footsteps | Impact Sounds | https://kenney.nl/assets/impact-sounds |
| Retro beeps, lasers, power-ups | Digital Audio | https://kenney.nl/assets/digital-audio |
| Short win and lose tunes | Music Jingles | https://kenney.nl/assets/music-jingles |

If a link doesn't open, search for the pack's name at kenney.nl. The pages are free; the "Continue without donating" link downloads the ZIP.

## Adding a pack (instructor)

1. Download the pack and unzip it.
2. Copy the unzipped folder here, keeping Kenney's folder name, such as `kenney_tiny-dungeon`.
3. Remove what Unity doesn't need: preview images, `.url` shortcuts and vector source files. Keep `License.txt`.
4. In Unity, choose **Collin Game → Art → Reapply Art Import Settings**. The importer sets each listed pack's pixels per unit and, for pixel art, sharp filtering. A new pack needs its own line in `CourseArtImporter`.
5. Add the pack to `Course Credits.txt`.

Kenney's art isn't part of the project download. Each kit's package brings the pack it needs.
