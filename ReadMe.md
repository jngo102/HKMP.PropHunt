# Prop Hunt

This mod is an HKMP add-on that adds the game mode "Prop Hunt" to Hollow Knight.

## Usage
1.  Ensure that you are connected to a server.
2.  Once all players are in the server, you may start the game using the `/prophunt` command.

## How to Play
- Once a game has started, players are assigned either to the team "Hunters" or to the team "Props".
### Hunters
- At the start of the round, you will get a black screen for a set number of seconds and be unable to move. This allows the props some time to get away and find a place to hide. Your goal is to kill all props before the round ends.
- You must be cautious about what background objects you hit. If you hit one that is not a player, you will take one mask of damage.
### Props
- You will have a set number of seconds to find a background object to change into and hide from the Hunters. Your goal is to survive until the round ends.
- You can change into most objects that are breakable background objects. The size of the object will determine how much health you have; the bigger the object is, the more health you have, but the more visible you are and the larger your hitbox is, so you may not fit in some spaces.
- You can select, translate, rotate, and scale your prop using a set of key inputs. These can be defined in the main menu or pause menu, under `Options > Mods > Prop Hunt`.
- If there are no breakable background objects nearby and you press the key to select a prop, then you will turn back into the Knight.
- You do not have the ability to counter-attack against Hunters as a Prop. You will only be able to hide and run away.

## Recommended Rules
### Hunters
1.  Do not use invincibility, infinite HP, or no clip to give yourself an unfair advantage.

### Props
1.  Do not use invincibility, infinite HP, or no clip to give yourself an unfair advantage.
2.  Make sure your prop is visible from some camera angle; do not hide your prop behind other objects.

## Commands
- /prophunt &lt;start|stop&gt; &lt;graceTime&gt; &lt;roundTime&gt;
    - *start|stop*: Either starts or stops a game of Prop Hunt.
    - *graceTime*: The length of time in seconds that Props have to find a place to hide before the Hunters begin hunting. Default is 15.
    - *roundTime*: The length of time in seconds that a round goes for. Default is 120 (2 minutes).
