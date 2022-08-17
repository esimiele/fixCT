# fixCT
####################################################################################
WARNING!
THIS CODE MODIFES PATIENT CT DATA, WHICH COULD IMPACT CLINICAL DECISION MAKING!
USE AT YOUR OWN RISK!!!
####################################################################################

fixCT is a tool used to modify patient CT dicom data to enable the needle autodetect functionality in Eclipse/Aria v16 (brachytherapy planning).
See the 'HDR brachytherapy needle autodetect workaround.docx' word document for a detailed description of the problem and the solution implemented in this tool.

In summary, this tool:
1. copies the patient CT data into a new folder
2. modifies the copied CT data to decrease the value of every pixel on every slice by 2^15 
3. the HU intercept in the dicom header is increased by 2^15. 
  --> There should be no net change in HU values!
4. Both CT sets, original and modified, should be imported and registered (to verify no change in the HU values)
  a. The patient ID and names are preserved in the modified CT data so when you point the import filter at the location of the original CT data, you can import both the original and modified data simultaneously.
5. Give the original CT to the physician for contouring and use the modified CT (where the autodetect tool works) for planning


What you need to do to get this tool to run:
1. download the source code onto your hospital computer and place it in a location that can be seen by aria/citrix
2. extract the source code
3. modify the fixCT_config.ini file (in the /bin folder) to list the path to the CT dicom data folder (i.e., the folder where the CT scanner exports the dicom images for import into Eclipse)
4. open Eclipse and select-->tools-->scripts-->folder-->change folder-->navigate to the bin directory where you placed the fixCT code-->hit ok
5. run launchFixCT.cs (a console window should pop up displaying all of the patients in the CT dicom data folder)
6. select a patient dataset on the console window (using numerical entry or 'n' to select the patient folder)
7. wait for the tool to finish modifying the data (hit enter in the console window when it's done)
8. In external beam planning-->file-->import-->import CT data-->select the patient whose data you modified (two CT datasets should show up)-->import both datasets
9. look at the CT properties in the contouring workspace and determine which CT is the modified and which is the original (the original CT will have a reported HU intercept of ~ -2^15 HU and the modified CT should have an intercept of ~ -1024 HU). Rename the datasets accordingly
10. register the two CT images together (same DICOM origin, no shifts required)
11. insert a new plan on the modified CT dataset in the brachytherapy planning workspace
12. insert a new applicator (F9)-->right click on the applicator-->Detect applicator from image
13. place the crosshairs on one of the needles and the image in the popup window should now make sense (verify that the slider bar on the popup window actually adjust the thresholding in the window)
14. be happy


####################################################################################
####################################################################################
Some important notes!
Should you choose to use this code, you understand and accept that:
1. I wrote this code for our specific situation at Rutgers University. No guarantees it will work out of the box at your institution 
2. I DON'T CLAIM THAT THIS TOOL WORKS AND I'M NOT RESPONSIBLE IF IT DOESN'T!!!
3. 
####################################################################################
####################################################################################

