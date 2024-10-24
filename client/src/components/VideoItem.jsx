import React from "react";
import "../components/VideoItem.scss";
import { Link } from "react-router-dom";

const VideoItem = ({ video }) => {
  const { id, title, duration, thumbnailPath } = video;

  const convertTime = (seconds) => {
    let hours = Math.floor(seconds / 3600);
    let minutes = Math.floor((seconds % 3600) / 60);
    let extraSeconds = seconds % 60;

    hours = hours < 10 ? "0" + hours : hours;
    minutes = minutes < 10 ? "0" + minutes : minutes;
    extraSeconds = extraSeconds < 10 ? "0" + extraSeconds : extraSeconds;

    return hours > 0
      ? `${hours}:${minutes}:${extraSeconds}`
      : `${minutes}:${extraSeconds}`;
  };

  return (
    <div className="col-md-4 video-item">
      <Link to={`/video/${id}`}>
        <h4>{title}</h4>
        <img
          src={`https://localhost:7124/api/Video/thumbnail/${thumbnailPath}`}
          alt={thumbnailPath}
        ></img>
        <p>Duration {convertTime(duration)}</p>
      </Link>
    </div>
  );
};

export default VideoItem;
