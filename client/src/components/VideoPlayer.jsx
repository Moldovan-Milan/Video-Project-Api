import React from "react";
import ReactPlayer from "react-player/lazy";

const VideoPlayer = ({ src }) => {
  return (
    <ReactPlayer
      style={{ width: "500px", height: "500px" }}
      controls={true}
      url={src}
      pip={true}
      stopOnUnmount={false}
    />
  );
};

export default VideoPlayer;
