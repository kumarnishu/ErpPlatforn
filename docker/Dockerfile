FROM node:22-alpine

ARG APP_DIR
WORKDIR /app

COPY webapp/${APP_DIR}/package.json webapp/${APP_DIR}/package-lock.json ./
RUN npm ci

COPY webapp/${APP_DIR}/ ./

EXPOSE 3000 3001 3002 3004

CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]
