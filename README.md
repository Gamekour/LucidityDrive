A starter kit for making immersive games in Unity.

Currently features the base movement system and a sample scene, though a huge code cleanup and modular item system are on their way

To install:
- in unity, go to the package manager (Window > Package Manager)
- click the + button at the top left of the package manager window, then "add from git url"
- copy and paste https://github.com/Gamekour/LucidityDrive.git into the field and confirm

To set up(eventually will be done with a setup script):
- open the project settings window (Edit > Project Settings)
- go to the Tags and Layers section, and create a layer called "Player"
- go to the Physics section, scroll down, and uncheck the box where the row "Player" intersects with the column "Player"

To use:
- drag the prefabs "PlayerRig", "VismodelExample", and "WorldInfo" into a scene

And that's it - more comprehensive documentation will be made in the future, but I figured I'd open the code up early on for anyone interested.
