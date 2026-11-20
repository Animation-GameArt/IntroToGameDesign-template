# Intro to Game Design — Unity Template Project

The starting Unity project for **GAME 1303 Intro to Game Design** at Collin College. It contains the course scripts and exercises. You build the exercises in it first, then your own game.

> **Status:** in development. Not ready for class use yet.

## What you need

- **Unity 6000.3.10f1** (Unity 6.3 LTS), installed through Unity Hub
- A laptop or an external SSD to keep the project on

You don't need Git or a GitHub account.

## Get the project

1. On this page, open **Releases** and download the latest project ZIP.
2. **Unzip it fully** to your laptop or SSD. Unity can't open a project from inside a ZIP.
3. Rename the folder to your own project name.
4. In Unity Hub, choose **Add → Add project from disk** and select the folder.

The first time the project opens, Unity builds its `Library` folder. This takes several minutes.

## Folders

| Folder | Whose | Contents |
|---|---|---|
| `Assets/_Course/` | The course | Core scripts, 2D and 3D scripts, genre kits. **Don't edit anything here.** |
| `Assets/_Game/` | Yours | Everything you make for your own game |
| `Assets/Scenes/Exercises/` | Yours | Your saved copies of the exercise scenes |

- To change a course script, copy it into `Assets/_Game/` and edit your copy.
- For each exercise, use **File → Save As…** to save your own copy of the scene in `Assets/Scenes/Exercises/`.

## Updates

Fixes and new material are published on the **Releases** page as `.unitypackage` files. Each update includes all earlier ones, so you only need the latest.

1. Download the latest update package.
2. With your project open in Unity, double-click the package, or choose **Assets → Import Package → Custom Package**.
3. Click **Import**.

Updates only change `Assets/_Course/`. Your own work is left alone. `Assets/_Course/VERSION.txt` shows which version you have.

## Back up your work

Choose one:

- **Unity Version Control:** in Unity Hub, open the project's menu and choose **Use Unity Version Control**. Check in at the end of every work session.
- **SSD:** work from your SSD, and copy the project folder to a second device at home once a week. Don't keep the project in a cloud-synced folder such as OneDrive.

## What's included

- Universal Render Pipeline (2D)
- Input System, Cinemachine, Timeline, 2D Tilemap Extras, TextMeshPro, UI Toolkit
- Course scripts in the `CollinGame` namespace

## License

[CC0 1.0 Universal](LICENSE). You may use, change and sell anything you build with this project, with no credit required.
