# Base image: node:18
FROM node:18 AS build
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm install
COPY . .
RUN npm run build --prod

# Use nginx:alpine for translation Angular
FROM nginx:alpine
COPY --from=build /app/dist/comment-system-client /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
