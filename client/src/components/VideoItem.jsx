import React from "react";
import "../components/VideoItem.scss";

const VideoItem = ({ video }) => {
  const { id, title, duration, thumbnailPath } = video;

  const convertTime = (seconds) => {
    let minutes = Math.floor(seconds / 60);
    let extraSeconds = seconds % 60;
    minutes = minutes < 10 ? "0" + minutes : minutes;
    extraSeconds = extraSeconds < 10 ? "0" + extraSeconds : extraSeconds;
    return `${minutes}:${extraSeconds}`;
  };

  return (
    <div className="videoItemContainer">
      <h4>{title}</h4>
      <img
        src={`https://localhost:7124/api/Video/thumbnail/${thumbnailPath}`}
        alt={thumbnailPath}
      ></img>
      <p>Duration {convertTime(duration)}</p>
    </div>
  );
};

export default VideoItem;
