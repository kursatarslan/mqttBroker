import React from "react";
// @ts-ignore
import MqttContext,{ useSubscription , useMqttState} from "mqtt-react-hooks";

const Status = () => {
    /*
     * Status list
     * - offline
     * - connected
     * - reconnecting
     * - closed
     */
    const {
        msgs,
        status,
        mqtt,
        lastMessageOnTopic,
        lastMessage,
        topic,
    } = useSubscription("platooning/followingVehicleReact/#");

    const handleClick = (message: string) => {
        mqtt.publish("platooning/leadvehicle", message);
    };
    return (
        <>
      <span>
        {`last message on mqtt: `}
          <strong>{JSON.stringify(lastMessage?.message)}</strong> topic:
        <strong> {lastMessage?.topic}</strong>
      </span>
            <h1>{`Status: ${status}; Host: ${mqtt?.options.host}; Protocol: ${mqtt?.options.protocol}; Topic: ${topic} `}</h1>
            <h2>{`last message on topic ${topic}: ${JSON.stringify(
                lastMessageOnTopic?.message
            )}`}</h2>
            <div style={{ display: "flex" }}>
                <button type="button" onClick={() => handleClick("enable")}>
                    Send Message
                </button>
                <button type="button" onClick={() => handleClick("disable")}>
                    Join Platoon
                </button>
            </div>
            <div style={{ display: "flex", flexDirection: "column" }}>
                {msgs?.map((message: { id: string | number | null | undefined; topic: any; message: any; }) => (
                    <span key={message.id}>
            {`topic:${message.topic} - message: ${JSON.stringify(
                message.message
            )}`}
          </span>
                ))}
            </div>
        </>
    );
};
export default Status;