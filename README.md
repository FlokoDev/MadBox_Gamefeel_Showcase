# Madbox — Game Feel Showcase

## Feedbacks implemented

In this short timespan, here are the simplest, yet impactful feedbacks I thought of implementing

### Hit flicker — 15–25 min.

-I Added Renderer flickers to entities' views on hit. It makes them flash when they receive a hit.
A script that controls MaterialPropertyBlocks, preserving textures and avoiding material copies.


### Animations — 45–65 min.

Animations were in the project, might aswell use 'em:

-Existing Idle, Move, Attack, Damage and Die clips were connected for the player and bees.
Movement stays controller-driven. Views call onto the animator.

-Attacks and hits use explicit events and orient the player toward the target.
Animator components (and animator behaviors) manage the transition priorities between animations.
*(Alternatives included inferring actions from state changes or managing transitions directly in the views, which I Never do without having a proper State machine. Might aswell use Unity's Animator HFSM)*

-Also had to change the controller to differenciate the concept of Dying Vs Despawning:
Dead bees leave combat immediately, while a controller-managed UniTask delay lets their animation play before visual removal.

### Floating damage numbers — 50–75 min, including pooling.

To display the inflicted damage, From player to  Bees or vice versa:
TMP numbers pop, rise and fade independently of their target.
Views choose their prefab (I.e Red for damage to the player, yellow to the bees)
A generic service reuses instances through a Core prefab pool.


### Curved bee approaches — 25–35 min.

Each bee keeps a left/right steering preference that fades near attack range.
Normalizing the direction preserves speed, and the model follows its movement. NavMesh, physics, flocking or any boids algorithm could offer avoidance, but I kept it simple.
I also tweaked the configs a bit.

The player is now faster than all bee variants. Cause this is a hit and run game. Every game of this genre (Vampire survivor-like) needs the player to be faster to be playable.
Attack ranges were adjusted


### Bonus : Had more time on my hands so I quickly implemented other feedbacks. Those are mostly for gamefeel and not very well coded:

- Tweens: added a PunchScale and DOMove on a bee hit, for knockback and juice
- AOE attacks: womp womp single attack games are boring. Added AOE on players' attacks 
- Couple of fixes here and there


### With more time ?

- Impact/death VFX and pitched attack/hit/death sounds. Needs proper Services to work well (and assets that I didn't have on the spot)
- Damage synchronized with the animation's impact frame. Not mandatory in games like these where attacks need to be instantaneous. But Could be a nice improvement to add impact to heavy weapons (imagine a heavy hammer that takes time to perform a heavy AOE => High risk, high reward)
- Player UI, enemy worldSpace UI, to display health, would be a minimum.


## Base project notes

Its a bit ambiguous as to what is considered feedback and what is considered a feature. So I did a bit of both.
Most of these implements usually use either famous assets (FEEL, Dotween, etc), or my own complex library (with my own service provider etc etc). I kept it simple by adding the strict minimum.
Not creating another view is intentionnal, since I also had to make changes to each controller. (Creating another view is more suited for AB testing, while this is a prototype)
(This is why I do not recommand MVCs on early projects : the view/controllers changes Everyday.)

Tried my best to fit the maximum into this 4H time span.
