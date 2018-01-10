---
layout: default
title: Home
---

## Posts

{% for post in site.posts %}
  {% if post.date | date: "%Y" != '2018' %}
    {{ post.date | date: "%d %b %Y" }}
    [ {{ post.title }} ]({{ post.url }})
  {% endif %}
{% endfor %}
