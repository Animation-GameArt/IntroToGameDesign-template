# 3D Scripts

Scripts for 3D and 2.5D games. They sit beside the 2D scripts in the same course project, and use the same Core scripts, UI and data.

| Script | What it does |
|---|---|
| `Movement/TankController3D` | A player turned by left and right and moved by forward and back, with jumping. For a camera behind the player |
| `Movement/RailRunner3D` | Keeps the player on a spline rail they run along, step sideways on and jump from |
| `Movement/PlayerController3D` | A player moved relative to the camera, with sprinting, crouching and jumping. Faces where it moves (third person) or where the camera looks (first person) |
| `Movement/MovingPlatform3D` | A platform that moves between points and carries the player |
| `Movement/BouncePad3D` | Launches the player upward |
| `Movement/Spin3D` | Spins and bobs a pickup |
| `Movement/CursorLock3D` | Hides and locks the mouse pointer while playing, for cameras turned by the mouse |
| `Minimap/Minimap3D` | An add-on: a top-down map in the corner of the screen (M), and a big map while Tab is held |
| `Minimap/MinimapMarker3D` | Shows an object on the map as a colored dot, square or arrow |

A 3D scene's camera uses the **Universal Renderer** (Main Camera → Rendering → Renderer). 2D scenes keep the 2D Renderer.

The tutorials on the course site, **2.5D Tutorial 1 — Third-Person Controller** and **2.5D Tutorial 2 — Rail Runner**, show how to use the first two. The **3D Platformer Kit** and **Stealth Kit** guides use the rest.

The **Minimap Add-On** guide on the course site sets up the map in any 3D game.
