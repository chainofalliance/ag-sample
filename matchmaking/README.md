# CoA Matchmaking

Implementation of our own matchmaking queue according to [this](https://chromaway.atlassian.net/wiki/spaces/CGD/pages/225018014/Battles+Matchmaking) design.

## Deployment

`cp .env.sample .env` and set the environment variables.

Init with `npm i`, then start with `npm start`.

## Testing

Create test tickets as defined in `src/test/create-ticket-test.ts` with `npm test`.

## Information

The ticket pool is currently in-memory and reset after restart. The benefit is better maintainability and easier deployment. But it lacks in scalability as we do cannot split modules over different nodes. Using a redis DB as backend for ticket pools would enable us to create containers for each route in order to add more resources for a component that is especially demanding.
