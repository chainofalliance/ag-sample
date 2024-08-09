import winston from "winston";

const { combine } = winston.format;

const colorizer = winston.format.colorize();

const logger = winston.createLogger({
    level: 'debug',
    format: combine(
        winston.format.timestamp(),
        winston.format.simple(),
        winston.format.printf(msg =>
            colorizer.colorize(msg.level, `${msg['timestamp']} - ${msg.level}: ${msg.message}`)
        )
    ),
    transports: [
        new winston.transports.Console(),
    ]

});

export { logger };
