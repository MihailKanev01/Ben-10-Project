# 📜 Ben 10: Omniverse - Unity Game


## 🌟 Project Overview  

This is a **3D action-adventure game** based on the *Ben 10* franchise, developed in Unity. Players control **Ben Tennyson** and can transform into various aliens using the Omnitrix, each with unique abilities and gameplay mechanics.

## ✨ Features  

- **🦸 Multiple Playable Aliens**: Transform into **Humungousaur, Way Big, Fasttrack,** and more!  
- **💥 Unique Abilities & Combat**:
  - **Humungousaur**: Size growth, ground pound attacks  
  - **Way Big**: Cosmic ray projectiles, stomp attacks  
  - **Fasttrack**: Super speed boost, quick dashes  
- **🔄 Transformation System**: Dynamic effects, timers, and cooldowns  
- **🌀 Alien Selection Wheel**: Intuitive UI for smooth alien switching  
- **🎥 Adaptive Camera System**: Adjusts based on alien size & abilities  
- **🎮 Smooth Third-Person Controls**: Running, jumping, and special moves  

## 🎮 Controls  

### **Basic Movement**
| Action  | Key |
|---------|------|
| Move    | **WASD** |
| Camera  | **Mouse** |
| Jump    | **Space** |
| Run     | **Left Shift** |

### **Ben 10 Transformation**
| Action  | Key |
|---------|------|
| Open Alien Selection Wheel | **Tab** |
| Transform/Revert | **T** |

### **Alien-Specific Controls**
| Action | Key |
|--------|------|
| Special Ability 1 | **Q** (Size growth, speed boost, etc.) |
| Special Ability 2 | **E** (Ground pound, dash, etc.) |
| Ranged Attack | **F** (Cosmic ray, etc.) |
| Extra Ability | **R** (Roar, etc.) |

---

## 🛠️ Technical Details  

- **Engine**: Unity  
- **Language**: C#  
- **Key Implementations**:
  - 🎮 Custom character controllers  
  - 🔄 Dynamic transformation system  
  - 🎥 Adaptive camera adjustments  
  - ✨ Special effects for abilities  
  - ⚡ Collision detection for attacks  
  - 📜 UI interaction systems  

---

## 📋 Requirements  

- **Unity Version**: 2021.3 LTS or newer  
- **Models & Animations**: (Not included in the repository)  

## ⚙️ Installation & Setup  

1. **Clone the repository**:  
   ```sh
   git clone https://github.com/MihailKanev01/Ben-10-Project.git
   
2.**Open in Unity**
   
3. **Set up required models & animations**:  
   - Add the main player object (`BenTennyson`) to the scene.  
   - Attach the `OmnitrixController` script to the player object.  
   - Import and set up alien models in the `Aliens` folder.  
   - Assign appropriate controller scripts to each alien:  
     - `HumungousaurController.cs`  
     - `WayBigController.cs`  
     - `FasttrackController.cs`  
   - Set up animations in Unity’s Animator and link them to the controllers.  

4. **Configure the alien selection wheel**:  
   - Open the `Canvas` object in the **UI hierarchy**.  
   - Locate the `AlienSelectionWheel` UI panel.  
   - Assign the available alien models to the selection slots in the **Inspector**.  

5. **Adjust player settings in the Inspector**:  
   - Open the `OmnitrixController` script in Unity's **Inspector**.  
   - Modify transformation cooldowns, duration, and ability values based on preferences.  
   - Adjust movement speed, jump height, and camera sensitivity as needed.  

6. **Configure camera system**:  
   - Attach the `FollowCamera` or `ThirdPersonCamera` script to the `Main Camera`.  
   - Ensure it smoothly follows **both Ben and his alien forms**.  
   - Adjust **camera zoom levels** based on alien size (e.g., zoom out for *Way Big*).  

7. **Test the transformation system**:  
   - Press **Play** in Unity and verify:  
     - Ben transforms correctly into different aliens.  
     - Each alien’s abilities work as expected.  
     - UI updates properly when selecting a new alien.  
     - Camera adjusts dynamically to alien sizes.  

8. **Adjust physics & collisions**:  
   - Ensure all character models have a **Rigidbody** component.  
   - Set up **BoxCollider** or **CapsuleCollider** for each alien.  
   - Configure collision layers to prevent unwanted interactions.  

9. **Save your changes** and commit them to GitHub:  
   ```sh
   git add .
   git commit -m "Initial Unity setup and transformations implemented"
   git push origin main


