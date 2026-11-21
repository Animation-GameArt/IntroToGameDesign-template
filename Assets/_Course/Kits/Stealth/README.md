# Stealth Kit

Scripts and a HUD for 3D stealth and horror games: guards or monsters that patrol, look and listen; places to hide; a flashlight with a battery; and an alert meter. The sample is a small building to sneak through, collecting three keys and reaching the exit.

| Folder | Contents |
|---|---|
| `Scripts/` | Guard 3D, Stealth Target 3D, Hiding Spot 3D, Flashlight 3D, Vision Cone 3D, Alert Audio, UI Stealth HUD |
| `UI/` | The stealth HUD: alert meter, state, objective, message and battery |
| `Sample/` | The sample level |

The kit uses the course's Core and 3D scripts (Player Controller 3D). Guards walk on a NavMesh, which needs Unity's **AI Navigation** package (Window → Package Manager → Unity Registry → AI Navigation).

Like everything in `_Course`, don't edit these files. Copy what you want to change into `Assets/_Game`.

The kit guide is on the course site: **Stealth Kit**.
