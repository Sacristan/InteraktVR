# InteraktVR
BEWARE -> this is not even in ALPHA -> might accidentaly set Your house on fire or accidentaly kidnap Your dog!

Supports:
* Vive
* Oculus Rift
* Oculus Quest

Probably works for:
* Valve Index (not tested)

# SETUP
It is supposed to be used as a submodule under Assets folder

### Import (at least one):
* https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022
* https://assetstore.unity.com/packages/tools/integration/steamvr-plugin-32647

### Add this as submodule (go to Assets foldeR)
```bash
git clone --recurse-submodules https://github.com/Sacristan/InteraktVR.git
cd InteraktVR && git submodule init && git submodule update && cd ..
```

# DESC
Consists of:
* modified: https://assetstore.unity.com/packages/tools/input-management/vr-interaction-119934
* WASD/Mouse Interaction system
* Some custom code 

None of the solutions are currently integrated with Unity event system.
