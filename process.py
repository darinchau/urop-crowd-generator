### Processes the dataset to export format and make a video about it :D
import os
import json
import cv2 as cv
import numpy as np
import matplotlib.pyplot as plt
import matplotlib.image as mpimg
import os
from moviepy.editor import *
from scipy import ndimage
from tqdm import tqdm
from multiprocessing import Pool
from abc import ABC, abstractmethod as virtual
import random
from p_tqdm import p_map

# Path to dataset
PATH = "./Data/Batch 43014179575"

# Draw a rectangle given the image, center, extents
# My favourite mint color :))))
COLOR = (62, 180, 137)
        

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

class VideoMaker:
    def modifier(self, img, frame_data):
        raise NotImplementedError
    
    # This texts will be appended every frame
    def get_texts(self, img, frame_data):
        return [
            f"Number of people: {frame_data['Number of people']}",
            f"Frame rate: {frame_data['Frame rate']}",
            f"Project link: https://github.com/darinchau/crowd_counting",
            f"Project link: https://github.com/darinchau/urop-crowd-generator"
        ]
    
    def save(self, path: str, nframes: int = 300):
        # Load the data
        with open(f"{PATH}/position.json", 'r') as f:
            st = f.read()
        datas = json.loads(st)

        # Process each frame by adding the bounding box
        def make_frame(i):
            img = mpimg.imread(f"{PATH}/Frame {i + 1}.png")
            img = np.array(img * 255, dtype = np.uint8)
            frame_data = datas[f"{i + 1}"]
            img = self.modifier(img, frame_data)
            frame_rate = frame_data["Frame rate"]

            font = cv.FONT_HERSHEY_SIMPLEX
            color = (255, 255, 255)
            imtext = np.zeros_like(img)
            for i, text in enumerate(self.get_texts(img, frame_data)):
                cv.putText(imtext, text, (80, 80 + 75 * i), fontFace = font, fontScale = 2, color = color, thickness=5)
            img[imtext == 255] = 255
            return img, frame_rate

        img_data = p_map(make_frame, range(nframes))
        self.make_vid(img_data, path)
        
    def make_vid(self, img_data, path):
        # Write to the video
        clips = [ImageClip(img).set_duration(1/frame_rate) for img, frame_rate in img_data]

        video = concatenate(clips, method="compose")
        video.write_videofile(path, fps=60)

# Adds bounding box to video
class BoundingBoxVideo(VideoMaker):
    def modifier(self, img, frame_data):
        bbs = [(x["bounding box left"], x["bounding box top"], x["bounding box right"], x["bounding box bottom"]) for x in frame_data["People position"]]
        bbs = np.array(bbs)
        new_img = np.array(img * 0.5, dtype = np.uint8)

        col = np.array(COLOR, dtype = np.uint8)

        for i in range(len(bbs)):
            lef, top, rig, bot = bbs[i]

            new_img[top, lef : rig, :] = col
            new_img[bot, lef : rig, :] = col
            new_img[top : bot, lef, :] = col
            new_img[top : bot, rig, :] = col
        
        return new_img
    
    def get_texts(self, img, frame_data):
        return [
            f"Number of people: {frame_data['Number of people']}",
            f"Frame rate: {frame_data['Frame rate']}",
            f"Project link: https://github.com/darinchau/crowd_counting",
            f"Project link: https://github.com/darinchau/urop-crowd-generator"
        ]

class DensityMapVideo(VideoMaker):
    def modifier(self, img, frame_data):
        # Extract the coordinates
        coords = [(x["x"], x["y"]) for x in frame_data["People position"]]

        # The radius (pixels) of each person in the density map
        sigma = 10

        # Modified from original code in preprocess.py
        # Turns coordinates to density image
        density = np.zeros((1080, 1920), dtype=np.float32)

        # Loop through every coordinates
        for x, y in coords:
            # Tranlate coordinates from (img size) into (actual size)
            a = int(y/2)
            b = int(x/2)
            pt2d = np.zeros((1080, 1920), dtype=np.float32)
            try:
                pt2d[a, b] = 1.
            except IndexError:
                pt2d[a-1, b-1] = 1.
            density += ndimage.gaussian_filter(pt2d, sigma, mode='constant')
        
        # Resize the image up to 4k
        density = cv.resize(density, (2160, 3840))

        # I know how to plot a heatmap but I dont know how to plot it to a numpy array, so use this hacky workaround
        # in multiprocessing I am worried about multiple images with the same name, hence make a random token
        r = random.randint(0, 99999999999999999999999999)
        path = f"./temp{r}.png"

        fig = plt.figure(frameon=False)
        fig.set_size_inches(3840/96, 2160/96)
        ax = plt.Axes(fig, [0., 0., 1., 1.])
        ax.set_axis_off()
        fig.add_axes(ax)
        ax.imshow(density, cmap = 'plasma', aspect='auto')

        # Save and load again
        fig.savefig(path, dpi = 96)
        new_img = mpimg.imread(path)
        new_img = np.array(new_img * 255, dtype=np.uint8)
        os.remove(path)
        plt.close(fig)

        return new_img

# Makes the original video with only a little bit of additional text
class PureVideo(VideoMaker):
    def modifier(self, img, frame_data):
        return img
    
    def get_texts(self, img, frame_data):
        return [
            f"Darin Chau - Crowd counting project (Simulated data in Unity 3D + design & implement CNN model)",
            "",
            f"Project link: https://github.com/darinchau/crowd_counting",
            f"Project link: https://github.com/darinchau/urop-crowd-generator"
        ]
    
class PurerVideo(VideoMaker):
    def modifier(self, img, frame_data):
        return img

if __name__ == "__main__":
    PurerVideo().save("vid.mp4")
    