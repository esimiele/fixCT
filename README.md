# fixCT
Tool used to fix CT to make Eclipse needle autodetect work for HDR treatment planning.

It basically decreases the value of every pixel on every slice by 2^15 and increases the HU intercerpt slope by 2^15. There should be no change in HU values, 
but this is enough to make the needle autodetect work
