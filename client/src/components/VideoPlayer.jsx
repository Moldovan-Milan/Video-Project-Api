import React from "react";

const VideoPlayer = ({ src }) => {
  return (
    <video
      style={{ width: "500px", height: "500px" }}
      controls
      src={src}
    ></video>
  );
};

export default VideoPlayer;
