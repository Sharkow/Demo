# Demo
Hello! These are some parts of my code from different projects.<br>
Some of them are from commercial software, so I merely share little pieces.<br>
#### DataGenerator [C#, 2012]
Just several parts from my graduation project.
This application allowes to specify flexible rules to generate random data with certain parameters.<br>
Along with simpler generators (like normally distributed data), there is a special "script" generator -<br>
the user can write his own script in C#, that would generate data by custom rules.<br>
DynamicCodeExecutorSH.cs - compiles such a script at runtime.<br>
StringScriptGenerator.cs - whole routines to use a custom script.<br>
DPPanel.cs - a base class for UI elements with data generators.<br>
Designed in a very generic way to enable extending the application with arbitrary generators.<br>
MainForm.cs - as you would think, the code of the main application window.<br>
#### ImageSphere.as [ActionScript 3, 2011]
Builds a live 3D-sphere of arbitrary images (passed as arguments).<br>
ImageSphere_render.png shows how result looks like.<br>
It was done in browser using video card for rendering.<br>
The code makes use of the spherical coordinate system to position images correctly.<br>
#### Main.java [Java, 2011]
This solves a mathematical problem from HeadHunter challenge.<br>
#### SHDataHandler.h/m, SHMainMenuViewController.h/m [Objective-C, 2014-2017]
Several parts from my iOS app, LR-West (can be found on the AppStore).<br>
SHDataHandler - routines for getting and storing data into DB, and some related stuff.<br>
SHMainMenuViewController - view controller for the main app screen.<br>
#### ai_split_message.h/cc [C++, 2018]
A module for splitting long messages into smaller ones.<br>
It is a part of a large project for airline industry.<br>
Messages contain information about people who ordered flight tickets, and are composed by certain standards.<br>
Splitting also follows some specific business logic.<br>
#### costFunction.m [Matlab, 2018]
A tiny piece from my homework in Matlab for Andrew Ng's ML course.<br>
#### main.c [C, 2016]
Full code of my fun project - a dancing Pololu 3pi robot (based on Arduino).<br>
This two-wheel robot will actually perform some dancing to a "Romeo and Juliette" song by Dire Straits.<br>
#### pfs_submit.ipynb, pfs_xgb.py [Python, 2019]
This is my solution for the "Predict Future Sales" Competition on Kaggle:<br>
https://www.kaggle.com/c/competitive-data-science-predict-future-sales<br>
pfs_submit.ipynb is a full solution in a Jupyther Notebook.<br>
pfs_xgb.py is part of an extended solution in pure Python. (xgb stands for XGBoost).<br>
