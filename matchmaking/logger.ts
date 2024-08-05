import { createLogger, transports, format } from "winston";

export const logger = createLogger({
    level: 'debug',
    format: format.combine(format.timestamp(), format.splat(), format.printf((info) => {
        const formattedDate = info['timestamp'].replace('T', ' ').replace('Z', '');
        return `${formattedDate} | ${info.level} | ${info.message}`;
    })),
    transports: [new transports.Console()],
});
