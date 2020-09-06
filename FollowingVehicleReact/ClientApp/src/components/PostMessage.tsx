import React,{Component} from "react";
// @ts-ignore
import { Connector,useMqttState } from "mqtt-react-hooks";
import Status from "./Status";
const Children = () => {
    const { status } = useMqttState();

    return <span>{status}</span>;
};

const options = {
    clean: true, // retain session
    connectTimeout: 4000, // Timeout period
    // Authentication information
    clientId: 'followingVehicleReact',
    hostname: 'localhost',
    port: 1883,
    protocol: 'mqtt',
    username: 'test',
    password: 'test',
    path: '/mqtt'
}
export class PostMessage extends React.Component {
    render() {
        return (
            <div>
                <Connector brokerUrl="mqtt://localhost:1883" opts={options}>
                    <Status/>
                    <Children />
                </Connector>
            </div>
        );
    }
}

export default PostMessage;