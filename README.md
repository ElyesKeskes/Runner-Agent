# Unity-MachineLearning-MiniProject
# Runner Agent Project

This project implements a reinforcement learning agent, **Runner Agent**, designed to navigate a path while avoiding obstacles and collecting rewards. The environment is randomized with obstacles and rewards, making each training episode unique.

## Agent Actions

The **Runner Agent** can perform the following actions:
- **Jump**: The agent can jump over obstacles.
- **Move on the X-axis**: The agent can move left and right across the path to avoid obstacles and collect rewards.

## Randomized Environment

The environment consists of a path with randomly placed obstacles and rewards. Obstacles can include various challenges that the agent must avoid. The rewards are randomly positioned along the X and Z axes, but their placement ensures they don’t overlap with obstacles or violate minimum spacing constraints.

### Key Environment Features:
- **Obstacle Randomization**: Obstacles are randomly placed along the path but maintain a minimum distance from each other.
- **Reward Randomization**: Rewards are scattered across the path, ensuring no overlap with obstacles and maintaining a safe distance from them.

## Installation Instructions

Follow the steps below to set up the environment and start training the agent:

### Step 1: Clone the Repository

Clone & Pull main branch of the repository

### Step 1: Install libraries in virtual environment

```
venv\Scripts\activate
python -m pip install --upgrade pip
pip3 install mlagents
pip3 install torch torchvision torchaudio
pip3 install protobuf==3.20.3
```


## Authors and acknowledgment
Hassène Zarrouk
Elyes Keskes
Belhadj Khelifa Med Aziz
Bella Kouka
Ghofrane Ben Jazia
Manel Ben Mansour

## License
Open source

## Project status
In progress
