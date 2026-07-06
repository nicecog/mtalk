FROM eclipse-temurin:21-jre-alpine

WORKDIR /app

RUN addgroup -S mtalk && adduser -S mtalk -G mtalk
RUN mkdir -p /app/uploads/mbench /app/uploads/mbody /app/data && chown -R mtalk:mtalk /app

COPY target/mtalk-server-1.0.0.jar app.jar

USER mtalk

EXPOSE 8080

ENV SERVER_PORT=8080
ENV SPRING_PROFILES_ACTIVE=docker

ENTRYPOINT ["java", "-jar", "app.jar"]
