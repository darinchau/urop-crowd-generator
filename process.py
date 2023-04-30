### Processes the dataset to export format and make a video about it :D
import os
import json
import h5py
import cv2 as cv
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.image as mpimg

# Path to dataset
PATH = "./Data/Batch 43014179575"

# Draw a rectangle given the image, center, extents
# My favourite mint color :))))
COLOR = (62, 180, 137)

# image is the image, bbs is the bounding boxes, in a numpy array of shape (N, 4)
# bbs in the order left, top, right, bottom
def check_bounding_box(img, bbs, base_brightness = 0.5):
    new_img = np.array(img, dtype = float)
    new_img *= base_brightness
    new_img = np.array(new_img, dtype = img.dtype)

    col = np.array(COLOR, dtype = np.uint8)

    for i in range(bbs.shape[0]):
        lef, top, rig, bot = bbs[i]

        new_img[top, lef : rig, :] = col
        new_img[bot, lef : rig, :] = col
        new_img[top : bot, lef, :] = col
        new_img[top : bot, rig, :] = col
    
    plt.figure()
    plt.plot(new_img)
    plt.show()

# Compress all the images into a compact h5 format and compress it to the max
# This h5 has the format
#    | Frame 1: data - (the image)
#    | Frame 2: data - (the image)
# etc
# The images have shape (2160, 3840, 3) in RGB format
# This takes about an hour
# Example of how to load and plot the first frame
# with h5py.File(f"{PATH}/data.h5") as f:
#     plt.figure()
#     plt.imshow(f["Frame 1"]['data'])
#     plt.show()
def compress_img():
    with h5py.File(f"{PATH}/data.h5", 'w') as f:
        i = 0
        for img_path in os.listdir(PATH):
            if not img_path.endswith(".png"):
                continue
            if i > 3:
                break
            image = mpimg.imread(f"{PATH}/{img_path}")
            image = np.array(image * 255, dtype = np.uint8)
            group = f.create_group(img_path[:-4])
            group.create_dataset("data", data = image, compression="gzip", compression_opts=9)
            print(f"Processed {img_path}")
            i += 1

# Performs string processing magic to turn the txt to json format
def make_data():
    with open(f"{PATH}/data.txt", 'r') as f:
        data = f.read()
    
    # Split all the data into frames first. The "frameend" is here exactly for this purpose. Discard anything after the last frameend, typically white space
    frs = data.split("frameend")[:-1]
    # Initialize empty dict to populate
    data_dict = {}

    # Loop through every frame
    for fr in frs:
        # Declare necessary variables
        frame_idx, num_people, frame_rate = 0, 0, 0
        frame_data = []

        # Split rows first
        frdata = fr.split("\n")
        for f in frdata:
            # Skip the empty lines
            if len(f.strip()) == 0:
                continue

            # If f begins with "Frame " then extract the frame number
            if f[:12] == "Frame rate: ":
                frame_rate = float(f.split("Frame rate: ")[1])
                continue

            if f[:6] == "Frame ":
                frame_idx = int(f.split("Frame ")[1].split(":")[0])
                continue

            if f[:13] == "Total count: ":
                num_people = int(f.split("Total count: ")[1])
                continue
            
            # We split each row at ":", then for each data we split again at "  ", then what's in between them would be the values we want
            # We put these values in an int constructor, and then put the whole loop in list comprehension, and cast it to a tuple
            t = tuple([int(f.split(":")[k].split("  ")[0]) for k in range(1,8)])
            this_person_data = {
                "id": t[2],
                "x": t[0],
                "y": t[1],
                "bounding box top": t[4],
                "bounding box left": t[3],
                "bounding box bottom": t[6],
                "bounding box right": t[5]
            }
            frame_data.append(this_person_data)
        
        data_dict[frame_idx] = {
            "Number of people": num_people,
            "Frame rate": frame_rate,
            "People position": frame_data
        }
    
    # DUmps data to json
    js = json.dumps(data_dict, indent = 4)
    with open(f"{PATH}/position.json", 'w') as f:
        f.write(js)

if __name__ == "__main__":
    # compress_img()
    