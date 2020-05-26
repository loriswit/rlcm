# Rayman Legends Challenge Manager

RLCM is a small application which includes multiple features related to Rayman Legends challenges.

- Install the **training room** mod and jump into any offline challenge.
- Choose any **challenge mode**, including the unused "against the clock" mode.
- Fetch and edit the **challenge seed**, making it possible to play past challenges again.
- Change the challenge **goal and limit** values.

## Usage

Launch RLCM and install the training room mod. Then, launch the game and jump into the "training" painting. You can then jump into any challenge and select a level generator from the list (this will define the difficulty and the default mode). Note that for some reason, the Murphy painting doesn't work, making this challenge unavailable.

Once the challenge is started, RLCM will display various kinds of information about the current challenge, most of which can be edited. The change of any property takes effect immediately, except for the seed which requires the challenge to be restarted.

If you jump into an **online** challenge, RLCM won't let you change any property, so that you can't use it to cheat against other players. You can still use it to read the challenge seed and other properties.

## Building

Navigate to the directory containing *Rlcm.sln* and run the following command:
```
msbuild /p:Configuration=Release
```
