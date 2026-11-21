# Kenney Art Packs

The kits' sample games use free art packs by Kenney (kenney.nl). Every pack is **CC0**: it can be used in any game, including commercial ones, without credit. The course credits Kenney anyway, in `Assets/_Course/Credits/Course Credits.txt`, which the game's Credits screen shows.

## Packs by kit

| Kit | Pack | Download |
|---|---|---|
| Platformer | Pixel Platformer | https://kenney.nl/assets/pixel-platformer |
| Top-down adventure | Tiny Dungeon | https://kenney.nl/assets/tiny-dungeon (also https://kenney-assets.itch.io/tiny-dungeon) |
| Top-down adventure | Tiny Town (outdoor areas) | https://kenney.nl/assets/tiny-town |
| Turn-based battle | Tiny Dungeon (the same pack as the adventure kit) | https://kenney.nl/assets/tiny-dungeon |
| Arcade classic: Breakout and Pong | Puzzle Pack 2 | https://kenney.nl/assets/puzzle-pack-2 |
| Arcade classic: Asteroids | Space Shooter Remastered | https://kenney.nl/assets/space-shooter-remastered |
| Physics puzzle | Physics Assets | https://kenney.nl/assets/physics-assets (also https://opengameart.org/content/physics-assets) |
| Top-down shooter | Pixel Shmup, or Space Shooter Remastered | https://kenney.nl/assets/pixel-shmup |
| Endless runner | Pixel Platformer (the same pack as the platformer kit) | https://kenney.nl/assets/pixel-platformer |
| Grid puzzle | Sokoban | https://kenney.nl/assets/sokoban |
| Tower defense | Tower Defense (top-down) | https://kenney.nl/assets/tower-defense-top-down |
| Card game | Playing Cards Pack | https://kenney.nl/assets/playing-cards-pack |
| Management and strategy, farming and life sim | Tiny Town, with the UI Pack | https://kenney.nl/assets/tiny-town |
| Menus and HUDs for any kit | Pixel UI Pack (or the UI Pack) | https://kenney.nl/assets/pixel-ui-pack |
| 3D kits | Platformer Kit, Prototype Textures | https://kenney.nl/assets/platformer-kit, https://kenney.nl/assets/prototype-textures |

## Sound packs

Sound packs are in the `audio` folder.

| Use | Pack | Download |
|---|---|---|
| Menu clicks and UI sounds | Interface Sounds, UI Audio | https://kenney.nl/assets/interface-sounds, https://kenney.nl/assets/ui-audio |
| Hits, impacts, footsteps | Impact Sounds | https://kenney.nl/assets/impact-sounds |
| Retro beeps, lasers, power-ups | Digital Audio, Sci-Fi Sounds | https://kenney.nl/assets/digital-audio, https://kenney.nl/assets/sci-fi-sounds |
| Swords, doors, coins, spells | RPG Audio | https://kenney.nl/assets/rpg-audio |
| Cards, chips and dice | Casino Audio | https://kenney.nl/assets/casino-audio |
| Spoken words ("Game over", "Round one", "Fight!") | Voiceover Pack, Voiceover Pack: Fighter | https://kenney.nl/assets/voiceover-pack, https://kenney.nl/assets/voiceover-pack-fighter |
| Short win and lose tunes | Music Jingles | https://kenney.nl/assets/music-jingles |

## In this folder

Pixel Platformer, Tiny Dungeon, Tiny Town, Physics Assets, Puzzle Pack 2, Space Shooter Remastered and Pixel UI Pack, and the sound packs above. Still to download: Pixel Shmup (top-down shooter) and Sokoban (grid puzzle).

If a link doesn't open, search for the pack's name at kenney.nl. The pages are free; the "Continue without donating" link downloads the ZIP.

## Adding a pack (instructor)

1. Download the pack and unzip it.
2. Copy the unzipped folder here, keeping Kenney's folder name, such as `kenney_tiny-dungeon`.
3. Remove what Unity doesn't need: `.url` shortcuts and the `Vector` folder. Keep `License.txt`. Sound packs go in the `audio` folder.
4. In Unity, choose **Collin Game → Art → Reapply Art Import Settings**. The importer sets each listed pack's pixels per unit and, for pixel art, sharp filtering. A new pack needs its own line in `CourseArtImporter`.
5. Add the pack to `Course Credits.txt`.

Kenney's art isn't part of the project download. Each kit's package brings the pack it needs.
