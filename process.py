### Processes the dataset to export format and make a video about it :D
import os
import json
import h5py
import cv2 as cv
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.image as mpimg
import os
from moviepy.editor import *
from scipy import ndimage
from tqdm import trange

# Path to dataset
PATH = "./Data/Batch 43014179575"

# Draw a rectangle given the image, center, extents
# My favourite mint color :))))
COLOR = (62, 180, 137)

# image is the image, bbs is the bounding boxes, in a numpy array of shape (N, 4)
# bbs in the order left, top, right, bottom
def add_bounding_box(img, bbs: list[tuple[int, int, int, int]], base_brightness = 0.5):
    new_img = np.array(img * base_brightness, dtype = np.uint8)

    col = np.array(COLOR, dtype = np.uint8)

    for i in range(len(bbs)):
        lef, top, rig, bot = bbs[i]

        new_img[top, lef : rig, :] = col
        new_img[bot, lef : rig, :] = col
        new_img[top : bot, lef, :] = col
        new_img[top : bot, rig, :] = col
    
    return new_img

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

# Creates a video for demonstration
def make_dem_video():
    # Load the data
    with open(f"{PATH}/position.json", 'r') as f:
        st = f.read()
    datas = json.loads(st)

    # Process each frame by adding the bounding box
    imgs = []
    frame_rates = []
    for i in trange(300, desc="Making demonstration video"):
        img = mpimg.imread(f"{PATH}/Frame {i + 1}.png")
        img = np.array(img * 255, dtype = np.uint8)

        # Add the bounding box
        frame_data = datas[f"{i + 1}"]
        bbs = [(x["bounding box left"], x["bounding box top"], x["bounding box right"], x["bounding box bottom"]) for x in frame_data["People position"]]
        bbs = np.array(bbs)
        img = add_bounding_box(img, bbs)

        # Add some text
        font = cv.FONT_HERSHEY_SIMPLEX
        color = (255, 255, 255)
        cv.putText(img, f"Number of people: {frame_data['Number of people']}", (80, 80), fontFace = font, fontScale = 2, color = color, thickness=5)
        cv.putText(img, f"Frame rate: {frame_data['Frame rate']}", (80, 155), fontFace = font, fontScale = 2, color = color, thickness=5)
        cv.putText(img, f"Project link: https://github.com/darinchau/crowd_counting", (80, 230), fontFace = font, fontScale = 2, color = color, thickness=5)
        cv.putText(img, f"Project link: https://github.com/darinchau/urop-crowd-generator", (80, 305), fontFace = font, fontScale = 2, color = color, thickness=5)

        imgs.append(img)
        frame_rates.append(frame_data["Frame rate"])
    
    # Write to the video
    clips = [ImageClip(imgs[i]).set_duration(1/frame_rates[i]) for i in range(len(imgs))]

    video = concatenate(clips, method="compose")
    video.write_videofile('test1.mp4', fps=60)

# Modified from original code in preprocess.py
# Turns coordinates to density image
def coords_to_density_map(coords, sigma=15):
    # sha is target output size
    density = np.zeros((2160, 3840), dtype=np.float32)

    # Loop through every coordinates
    for i in trange(len(coords)):
        # Tranlate coordinates from (img size) into (actual size)
        x = int(coords[i][1])
        y = int(coords[i][0])
        pt2d = np.zeros((2160, 3840), dtype=np.float32)
        try:
            pt2d[x, y] = 1.
        except:
            pt2d[x-1, y-1] = 1.
        density += ndimage.gaussian_filter(pt2d, sigma, mode='constant')

    # I know how to plot a heatmap but I dont know how to plot it to a numpy array, so use this hacky workaround
    fig = plt.figure(frameon=False)
    fig.set_size_inches(3840/96, 2160/96)
    ax = plt.Axes(fig, [0., 0., 1., 1.])
    ax.set_axis_off()
    fig.add_axes(ax)
    ax.imshow(density, cmap = 'hot', aspect='auto')

    # Save and load again
    fig.savefig("./temp.png", dpi = 96)
    new_img = mpimg.imread("./temp.png")
    new_img = np.array(new_img * 255, dtype=np.uint8)
    os.remove("temp.png")
    plt.close('all')

    return new_img

# Make demonstration video about the density
def make_dem_video_heatmap():
    with open(f"{PATH}/position.json", 'r') as f:
        st = f.read()
    datas = json.loads(st)

    # Process each frame by adding the bounding box
    imgs = []
    frame_rates = []
    for i in trange(300):
        # Add the bounding box
        frame_data = datas[f"{i + 1}"]
        coords = [(x["x"], x["y"]) for x in frame_data["People position"]]
        coords = np.array(coords)
        img = coords_to_density_map(coords)

        # Add some text
        font = cv.FONT_HERSHEY_SIMPLEX

        imtext = np.zeros_like(img)
        cv.putText(imtext, f"Number of people: {frame_data['Number of people']}", (80, 80), fontFace = font, fontScale = 2, color = (255, 255, 255), thickness=5)
        cv.putText(imtext, f"Frame rate: {frame_data['Frame rate']}", (80, 155), fontFace = font, fontScale = 2, color = (255, 255, 255), thickness=5)
        cv.putText(imtext, f"Project link: https://github.com/darinchau/crowd_counting", (80, 230), fontFace = font, fontScale = 2, color = (255, 255, 255), thickness=5)
        cv.putText(imtext, f"Project link: https://github.com/darinchau/urop-crowd-generator", (80, 305), fontFace = font, fontScale = 2, color = (255, 255, 255), thickness=5)

        img[imtext == 255] = 255

        imgs.append(img)
        frame_rates.append(frame_data["Frame rate"])
    
    # Write to the video
    clips = [ImageClip(imgs[i]).set_duration(1/frame_rates[i]) for i in range(len(imgs))]

    video = concatenate(clips, method="compose")
    video.write_videofile('density.mp4', fps=60)

if __name__ == "__main__":
    make_dem_video()
    make_dem_video_heatmap()
    