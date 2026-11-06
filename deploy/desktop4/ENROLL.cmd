@echo off
REM Enroll this PC as Desktop4 (named Node4, Mitsubishi 28546 P2/P3).
REM Does not start Titan.ChiefOfStaff.exe. Does not open Eleads. Does not type a password.
call "%~dp0..\cluster-node\ENROLL-NODE.cmd" Desktop4
